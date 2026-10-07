// Payments Worker host — Phase 0 scaffold only.
// Broker consumption, inbox pipeline and simulated payment authorization are introduced in
// PLAN.md Phases 5 and 9. No hosted services or business behavior are registered here;
// the host starts and then waits for a shutdown signal (Ctrl+C/SIGTERM) while doing
// no work — by design for the Phase 0 scaffold.
var builder = Host.CreateApplicationBuilder(args);
var host = builder.Build();
host.Run();
