# Modules

The planned modules are Identity, Users, Vehicles, Trips, RideRequests, Matching,
Notifications, and Reviews. Each folder is deliberately empty except for its marker.

When implementing a module, keep its endpoints, use cases, entities, and EF entity
configurations together. Register its dependencies and endpoints through simple
extension methods called from Program.cs. Start with concrete services; introduce
interfaces only where they provide an actual boundary or test seam.

Use the shared Infrastructure/AppDbContext and EF transactions initially. Modules
own their tables and expose operations; do not have one module directly mutate
another module's entities. Avoid generic repositories, mediator frameworks, and
separate assemblies until a concrete requirement justifies them.
