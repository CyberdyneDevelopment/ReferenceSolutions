using Fdw.Results;
using Hangfire;

namespace FdwReferenceSchedulers.Hangfire.Translators;

/// <summary>
/// The native call a Hangfire translator produces. Hangfire's own API is heterogeneous across
/// operations (<c>AddOrUpdate</c>, <c>RemoveIfExists</c>, <c>TriggerJob</c>, ...) — unlike SQL, where
/// every operation ends in one <c>SqlCommand</c> shape — so the translated value is the call itself,
/// closed over whatever it needs, ready for the service to invoke against the live
/// <see cref="IRecurringJobManager"/>. Hangfire's manager calls are synchronous.
/// </summary>
/// <param name="manager">The live Hangfire recurring-job manager to act against.</param>
public delegate IGenericResult HangfireNativeAction(IRecurringJobManager manager);
