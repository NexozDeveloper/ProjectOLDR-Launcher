Project OLDR — Standalone Game Client & Launcher

Welcome to the official repository of Project OLDR, a standalone launcher and game management client built for a custom multiplayer build of Secret Neighbor (2020 Edition).

This project was built from scratch using C# / .NET 10 and Windows Forms, with a strong focus on high-performance vector rendering, an install-less end-user flow, and real-time social networking backed by a cloud infrastructure.

Key Highlights

Install-less Single-File Deployment: Distributed as a single, portable ProjectOLDR.exe executable. On first launch, it self-installs silently into %appdata%\Nexoz, creates a desktop shortcut, and runs without leaving DLL clutter or requiring third-party setup installers.

Hardware-Agnostic GDI+ Vector UI: Completely custom dark interface with rounded surfaces, neon status pills, and hand-drawn vector iconography (IconRenderer) avoiding the need for external asset packs or missing font fallbacks.

Real-Time Presence & Analytics: Direct database integration maintaining a 3-second heartbeat to track active clients and render an accurate, live counter of connected players.

Decoupled Social Hub: Clean friend management enabling users to verify target accounts in the database, dispatch invitations, inspect live online/offline states, and accept or reject requests.

Automated Versioning Pipeline: Integrated semantic release checker querying the GitHub REST API to detect, compare, and flag newer application tags against local client builds.

Embedded Game Handling: Archive validation, automatic decompression of game assets into %appdata%, and native process supervision.

Building from Source
Prerequisites
.NET 10.0 SDK
Visual Studio 2026 or newer (with the .NET desktop development workload enabled)
A configured EmbeddedConfig.xml containing your supabase database keys embedded as a resource

Compilation Command

To produce the single-file, portable binary directly without external libraries:

dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true

The resulting file will be located at:

C:\Users\YourName\source\repos\ProjectOLDR\bin\Release\net10.0-windows\win-x64\publish\ProjectOLDR.exe

License

This software is released under the terms specified in the LICENSE file. Review the license before redistribution, branching, or commercial application.

bin\Release\net10.0-windows\win-x64\publish\ProjectOLDR.exe

End
