# RSecurityBackend
Basic security backend including user management and some general utilities for ASP.NET Core applications

A basic sample application is available here [RService](https://github.com/hrmoh/RService)

As a real and working client application you may check the RMuseum project at [GanjoorService](https://github.com/ganjoor/GanjoorService)

# Note
If you are upgrading from a version prior to 1.2.2 you should add these additional lines to your program.cs:

//workspace service

builder.Services.AddTransient<IWorkspaceService, WorkspaceService>();

//workspace role service (you actually need to derive your own class from WorkspaceRolesServiceBase to have you own GetSecurableItems)

builder.Services.AddTransient<IWorkspaceRolesService, WorkspaceRolesServiceBase>();
