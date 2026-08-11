using Xunit;

// WebApplicationFactory captures the host via a process-wide resolver that is not
// concurrency-safe for top-level-statement entry points. Each test class also boots its
// own host + database, so run them sequentially.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
