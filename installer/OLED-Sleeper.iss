; Inno Setup installer script for ScreenKeeper
; Builds a single installer supporting both x64 and x86 deployments.

#include "CodeDependencies.iss"

; The build script passes the MinVer-derived version as /DAppVersion=. This fallback only applies to a
; compile started from the Inno Setup IDE.
#ifndef AppVersion
  #define AppVersion "0.0.0-dev"
#endif

[Setup]
; Unique application identifier used by Windows for installation tracking.
AppId={{5628C27B-72F7-4523-9931-A132AC8E407D}}

; Application metadata shown in the installer and system UI.
AppName=多屏管家 ScreenKeeper
UninstallDisplayName=多屏管家 ScreenKeeper
AppVersion={#AppVersion}

; Installation runs without elevation.
PrivilegesRequired=lowest

; The mutex ApplicationInstanceManager holds. Setup and uninstall both wait for the application to be closed,
; so its own shutdown restores monitor brightness before any file is replaced or removed.
AppMutex=ScreenKeeper-Mutex

; Output installer configuration.
OutputBaseFilename=ScreenKeeper-{#AppVersion}-Setup
SourceDir=.
OutputDir=..\artifacts

; Default installation directory.
DefaultDirName={autopf}\ScreenKeeper

; Enables 64-bit installation mode when running on x64 systems.
ArchitecturesInstallIn64BitMode=x64compatible

; Installer and uninstall entry icons.
SetupIconFile=..\OLED-Sleeper\Assets\icon.ico
UninstallDisplayIcon={app}\ScreenKeeper.exe

; General installer UI and compression settings.
DefaultGroupName=ScreenKeeper
AllowNoIcons=yes
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
; Default language configuration.
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
; Optional installation tasks presented to the user.
Name: "startup"; Description: "Launch ScreenKeeper when Windows starts"; GroupDescription: "Additional options:";
Name: "desktopicon"; Description: "Create a desktop icon"; GroupDescription: "Additional shortcuts:";

; Preserve settings and brightness recovery records on reinstall. Never touch upstream data.

[Files]
; Install platform-specific binaries based on system architecture.
Source: "..\artifacts\publish\x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: Is64BitInstallMode
Source: "..\artifacts\publish\x86\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: not Is64BitInstallMode

[Icons]
; Start Menu and optional Desktop shortcuts.
Name: "{group}\多屏管家 ScreenKeeper"; Filename: "{app}\ScreenKeeper.exe"
Name: "{autodesktop}\多屏管家 ScreenKeeper"; Filename: "{app}\ScreenKeeper.exe"; Tasks: desktopicon

[Registry]
; Optional autostart entry created when the startup task is selected.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "ScreenKeeper"; ValueData: """{app}\ScreenKeeper.exe"" -h"; Flags: uninsdeletevalue; Tasks: startup

[Run]
; Optionally launch the application after installation completes.
Filename: "{app}\ScreenKeeper.exe"; Description: "{cm:LaunchProgram,ScreenKeeper}"; Flags: nowait postinstall skipifsilent

[Code]

// Reports whether the application's autostart registry entry exists.
function StartupIsEnabled(): Boolean;
begin
  Result := RegValueExists(HKEY_CURRENT_USER, 'Software\Microsoft\Windows\CurrentVersion\Run', 'ScreenKeeper');
end;

// Ticks the startup task when autostart is already enabled, whether it was enabled here or from the
// application's own toggle. Unlisted tasks keep their state.
procedure InitializeWizard();
begin
  if StartupIsEnabled() then
  begin
    WizardSelectTasks('startup');
  end;
end;

// Removes the application's autostart registry entry if present.
procedure RemoveStartupKey();
begin
  if RegValueExists(HKEY_CURRENT_USER, 'Software\Microsoft\Windows\CurrentVersion\Run', 'ScreenKeeper') then
  begin
    Log('Removing startup registry key.');
    RegDeleteValue(HKEY_CURRENT_USER, 'Software\Microsoft\Windows\CurrentVersion\Run', 'ScreenKeeper');
  end;
end;

// Clears the autostart entry during installation only when the startup task was left unticked. The
// [Registry] entry writes it back when the task is ticked.
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep = ssInstall) and (not WizardIsTaskSelected('startup')) then
  begin
    RemoveStartupKey();
  end;
end;

// Runs during uninstallation to ensure the autostart entry is removed.
procedure CurUninstallStepChanged(UninstallStep: TUninstallStep);
begin
  if UninstallStep = usUninstall then
  begin
    RemoveStartupKey();
  end;
end;

// Requests download of required runtime dependencies before installation begins.
function InitializeSetup(): Boolean;
begin
  Dependency_AddDotNet80Desktop;
  Result := True;
end;