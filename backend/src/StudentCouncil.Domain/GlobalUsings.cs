global using StudentCouncil.Domain.Enums;

// The spec names the work-item status enum `TaskStatus`, which collides with
// System.Threading.Tasks.TaskStatus (imported via ImplicitUsings). This alias
// makes the unqualified name always resolve to our domain enum.
global using TaskStatus = StudentCouncil.Domain.Enums.TaskStatus;
