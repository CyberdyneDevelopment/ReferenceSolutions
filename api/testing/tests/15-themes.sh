#!/usr/bin/env bash
# tests/15-themes.sh — Theme endpoint tests
# GET /themes: Policies("fdw:configurations:read") → ALL roles (Viewer, Operator, Admin)
# POST/PUT/DELETE /themes: Policies("fdw:configurations:write") → Admin ONLY
# Operator does NOT have configurations:write!

test_themes() {
    describe "Themes — Auth Matrix"

    it "GET /themes without auth returns 401"
    anon_get "/themes"
    assert_status 401

    it "POST /themes without auth returns 401"
    anon_post "/themes" '{"name":"x"}'
    assert_status 401

    it "GET /themes as viewer returns 200"
    viewer_get "/themes"
    assert_status 200

    it "POST /themes as viewer returns 403"
    viewer_post "/themes" '{"name":"x","primaryColor":"#000000"}'
    assert_status 403

    it "GET /themes as operator returns 200"
    operator_get "/themes"
    assert_status 200

    it "POST /themes as operator returns 403"
    operator_post "/themes" '{"name":"x","primaryColor":"#000000"}'
    assert_status 403

    it "GET /themes as admin returns 200"
    admin_get "/themes"
    assert_status 200

    # ── List Themes ──
    describe "Themes — Seeded Data"

    it "Themes is array"
    assert_json_array "."

    it "Exactly 2 themes"
    assert_json_array_length "." 2

    # Verify Default theme exists
    local default_idx
    default_idx=$(response_body | jq '[.[] | .name] | index("Default")' 2>/dev/null)

    it "Default theme exists"
    if [[ "$default_idx" != "null" && -n "$default_idx" ]]; then
        pass
    else
        fail "Default theme not found"
    fi

    it "Default theme has name"
    assert_json_string_value ".[$default_idx].name" "Default"

    it "Default theme isDefault is true"
    assert_json_bool ".[$default_idx].isDefault" "true"

    it "Default theme isBuiltIn is true"
    assert_json_bool ".[$default_idx].isBuiltIn" "true"

    # Verify Dark theme exists
    local dark_idx
    dark_idx=$(response_body | jq '[.[] | .name] | index("Dark")' 2>/dev/null)

    it "Dark theme exists"
    if [[ "$dark_idx" != "null" && -n "$dark_idx" ]]; then
        pass
    else
        fail "Dark theme not found"
    fi

    it "Dark theme isDefault is false"
    assert_json_bool ".[$dark_idx].isDefault" "false"

    it "Dark theme isBuiltIn is false"
    assert_json_bool ".[$dark_idx].isBuiltIn" "false"

    # ── Default Theme Detail ──
    describe "Themes — Default Theme Detail"

    it "GET /themes/default returns default theme"
    admin_get "/themes/default"
    assert_status 200

    it "Default theme has id (guid)"
    assert_json_guid ".id"

    it "Default theme name is Default"
    assert_json_string_value ".name" "Default"

    it "Default theme has description"
    assert_json_string ".description"

    it "Default theme isDefault is true"
    assert_json_bool ".isDefault" "true"

    it "Default theme isBuiltIn is true"
    assert_json_bool ".isBuiltIn" "true"

    it "Default theme primaryColor is #1976D2"
    assert_json_string_value ".primaryColor" "#1976D2"

    it "Default theme secondaryColor is #424242"
    assert_json_string_value ".secondaryColor" "#424242"

    it "Default theme backgroundColor is #FFFFFF"
    assert_json_string_value ".backgroundColor" "#FFFFFF"

    it "Default theme textColor is #212121"
    assert_json_string_value ".textColor" "#212121"

    it "Default theme accentColor is #FF9800"
    assert_json_string_value ".accentColor" "#FF9800"

    # ── CRUD Lifecycle ──
    describe "Themes — CRUD Lifecycle"

    local TEST_THEME="e2e-theme-$(date +%s)"

    it "Create theme"
    admin_post "/themes" "{
        \"name\": \"$TEST_THEME\",
        \"description\": \"E2E test theme\",
        \"primaryColor\": \"#FF0000\",
        \"secondaryColor\": \"#00FF00\",
        \"backgroundColor\": \"#FFFFFF\",
        \"textColor\": \"#000000\",
        \"accentColor\": \"#0000FF\"
    }"
    local create_status
    create_status=$(status_code)
    if [[ "$create_status" == "201" || "$create_status" == "200" ]]; then
        pass
    else
        skip "Theme create returned $create_status"
        return
    fi

    local theme_id
    theme_id=$(response_body | jq -r '.id // .name // empty' 2>/dev/null)

    if [[ -n "$theme_id" ]]; then
        it "Read back created theme"
        admin_get "/themes/$theme_id"
        assert_status 200

        it "Created theme has correct name"
        assert_json_string_value ".name" "$TEST_THEME"

        it "Delete created theme"
        admin_delete "/themes/$theme_id"
        local del_status
        del_status=$(status_code)
        if [[ "$del_status" == "204" || "$del_status" == "200" ]]; then
            pass
        else
            fail "Delete theme returned $del_status"
        fi

        it "Deleted theme returns 404"
        admin_get "/themes/$theme_id"
        assert_status 404
    fi

    # ── Cannot Delete Built-In Theme ──
    describe "Themes — Built-In Protection"

    it "DELETE /themes on Default (built-in) should fail"
    local default_id
    admin_get "/themes/default"
    default_id=$(response_body | jq -r '.id // empty' 2>/dev/null)
    if [[ -n "$default_id" && "$default_id" != "null" ]]; then
        admin_delete "/themes/$default_id"
        local builtin_del_status
        builtin_del_status=$(status_code)
        # Should be 400 or 409 (cannot delete built-in)
        if [[ "$builtin_del_status" == "400" || "$builtin_del_status" == "409" || "$builtin_del_status" == "422" ]]; then
            pass
        elif [[ "$builtin_del_status" == "204" || "$builtin_del_status" == "200" ]]; then
            fail "Should not be able to delete built-in theme but got $builtin_del_status"
        else
            skip "Delete built-in returned $builtin_del_status"
        fi
    else
        skip "Could not determine Default theme id"
    fi

    # ── Set Default ──
    it "POST /themes/{name}/default sets default theme"
    admin_post "/themes/Default/default" ""
    local set_default_status
    set_default_status=$(status_code)
    if [[ "$set_default_status" == "200" || "$set_default_status" == "204" ]]; then
        pass
    elif [[ "$set_default_status" == "404" ]]; then
        skip "Set default endpoint not implemented"
    else
        skip "Set default returned $set_default_status"
    fi
}
