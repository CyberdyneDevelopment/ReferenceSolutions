#!/usr/bin/env bash
# lib/assertions.sh — jq-based response assertions
# All assertions are safe under set -euo pipefail (no unguarded non-zero exits)

# ── Status Code ──
assert_status() {
    local expected="$1"
    local actual
    actual=$(status_code) || true
    if [[ "$actual" == "$expected" ]]; then
        pass
    else
        fail "expected status $expected, got $actual (body: $(response_body 2>/dev/null | head -c 200))"
    fi
}

# ── JSON Property Existence ──
assert_json_property() {
    local path="$1"
    local val
    val=$(response_body | jq -e "$path" 2>/dev/null) || true
    if [[ -n "$val" && "$val" != "null" ]]; then
        pass
    else
        fail "expected property $path to exist and not be null"
    fi
}

# ── String Assertions ──
assert_json_string() {
    local path="$1"
    local val
    val=$(response_body | jq -r "($path) | type" 2>/dev/null) || true
    if [[ "$val" == "string" ]]; then
        pass
    else
        fail "expected $path to be string, got $val"
    fi
}

assert_json_string_value() {
    local path="$1"
    local expected="$2"
    local actual
    actual=$(response_body | jq -r "$path" 2>/dev/null) || true
    if [[ "$actual" == "$expected" ]]; then
        pass
    else
        fail "expected $path == \"$expected\", got \"$actual\""
    fi
}

assert_json_string_contains() {
    local path="$1"
    local substring="$2"
    local actual
    actual=$(response_body | jq -r "$path" 2>/dev/null) || true
    if [[ "$actual" == *"$substring"* ]]; then
        pass
    else
        fail "expected $path to contain \"$substring\", got \"$actual\""
    fi
}

# ── Number Assertions ──
assert_json_number() {
    local path="$1"
    local val
    val=$(response_body | jq -r "($path) | type" 2>/dev/null) || true
    if [[ "$val" == "number" ]]; then
        pass
    else
        fail "expected $path to be number, got $val"
    fi
}

assert_json_number_value() {
    local path="$1"
    local expected="$2"
    local actual
    actual=$(response_body | jq -r "$path" 2>/dev/null) || true
    if [[ "$actual" == "$expected" ]]; then
        pass
    else
        fail "expected $path == $expected, got $actual"
    fi
}

assert_json_number_min() {
    local path="$1"
    local min="$2"
    local actual
    actual=$(response_body | jq -r "$path" 2>/dev/null) || true
    if [[ -n "$actual" && "$actual" != "null" ]] && [[ "$actual" -ge "$min" ]] 2>/dev/null; then
        pass
    else
        fail "expected $path >= $min, got $actual"
    fi
}

# ── Boolean Assertions ──
assert_json_bool() {
    local path="$1"
    local expected="${2:-}"
    local val
    val=$(response_body | jq -r "($path) | type" 2>/dev/null) || true
    if [[ "$val" != "boolean" ]]; then
        fail "expected $path to be boolean, got $val"
        return
    fi
    if [[ -n "$expected" ]]; then
        local actual
        actual=$(response_body | jq -r "$path" 2>/dev/null) || true
        if [[ "$actual" == "$expected" ]]; then
            pass
        else
            fail "expected $path == $expected, got $actual"
        fi
    else
        pass
    fi
}

# ── Array Assertions ──
assert_json_array() {
    local path="$1"
    local val
    val=$(response_body | jq -r "($path) | type" 2>/dev/null) || true
    if [[ "$val" == "array" ]]; then
        pass
    else
        fail "expected $path to be array, got $val"
    fi
}

assert_json_array_length() {
    local path="$1"
    local expected="$2"
    local actual
    actual=$(response_body | jq -r "($path) | length" 2>/dev/null) || true
    if [[ "$actual" == "$expected" ]]; then
        pass
    else
        fail "expected $path length == $expected, got $actual"
    fi
}

assert_json_array_min_length() {
    local path="$1"
    local min="$2"
    local actual
    actual=$(response_body | jq -r "($path) | length" 2>/dev/null) || true
    if [[ -n "$actual" && "$actual" != "null" ]] && [[ "$actual" -ge "$min" ]] 2>/dev/null; then
        pass
    else
        fail "expected $path length >= $min, got $actual"
    fi
}

assert_json_array_contains() {
    local path="$1"
    local value="$2"
    local found
    found=$(response_body | jq "($path) | index(\"$value\")" 2>/dev/null) || true
    if [[ -n "$found" && "$found" != "null" ]]; then
        pass
    else
        fail "expected $path to contain \"$value\""
    fi
}

# ── Object Assertions ──
assert_json_object() {
    local path="$1"
    local val
    val=$(response_body | jq -r "($path) | type" 2>/dev/null) || true
    if [[ "$val" == "object" ]]; then
        pass
    else
        fail "expected $path to be object, got $val"
    fi
}

# ── GUID Assertion ──
assert_json_guid() {
    local path="$1"
    local val
    val=$(response_body | jq -r "$path" 2>/dev/null) || true
    if [[ "$val" =~ ^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$ ]]; then
        pass
    else
        fail "expected $path to be GUID, got \"$val\""
    fi
}

# ── DateTime Assertion (ISO 8601) ──
assert_json_datetime() {
    local path="$1"
    local val
    val=$(response_body | jq -r "$path" 2>/dev/null) || true
    if [[ "$val" =~ ^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2} ]]; then
        pass
    else
        fail "expected $path to be ISO 8601 datetime, got \"$val\""
    fi
}

# ── Null Assertions ──
assert_json_null() {
    local path="$1"
    local val
    val=$(response_body | jq -r "$path" 2>/dev/null) || true
    if [[ "$val" == "null" ]]; then
        pass
    else
        fail "expected $path to be null, got \"$val\""
    fi
}

assert_json_not_null() {
    local path="$1"
    local val
    val=$(response_body | jq "$path" 2>/dev/null) || true
    if [[ -n "$val" && "$val" != "null" ]]; then
        pass
    else
        fail "expected $path to not be null"
    fi
}

# ── Body Assertions ──
assert_body_empty() {
    local body
    body=$(response_body) || true
    if [[ -z "$body" || "$body" == "" ]]; then
        pass
    else
        fail "expected empty body, got \"$(echo "$body" | head -c 100)\""
    fi
}

assert_body_not_empty() {
    local body
    body=$(response_body) || true
    if [[ -n "$body" && "$body" != "" ]]; then
        pass
    else
        fail "expected non-empty body"
    fi
}

# ── Secret Masking ──
assert_json_masked() {
    local path="$1"
    local val
    val=$(response_body | jq -r "$path" 2>/dev/null) || true
    if [[ "$val" == "••••••••" || "$val" == "********" ]]; then
        pass
    else
        fail "expected $path to be masked, got \"$val\""
    fi
}

# ── Composite Shape Assertions ──
assert_paginated_shape() {
    local items_ok total_ok page_ok
    items_ok=$(response_body | jq 'if .items | type == "array" then "true" else "false" end' 2>/dev/null) || true
    total_ok=$(response_body | jq 'if .totalCount | type == "number" then "true" else "false" end' 2>/dev/null) || true
    page_ok=$(response_body | jq 'if .page | type == "number" then "true" else "false" end' 2>/dev/null) || true
    if [[ "$items_ok" == '"true"' && "$total_ok" == '"true"' && "$page_ok" == '"true"' ]]; then
        pass
    else
        fail "expected paginated shape {items[], totalCount, page, pageSize, totalPages}"
    fi
}
