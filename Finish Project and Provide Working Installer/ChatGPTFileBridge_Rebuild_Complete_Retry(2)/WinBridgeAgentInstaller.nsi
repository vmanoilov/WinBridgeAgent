; WinBridgeAgent Installer Script
; Part of WinBridgeAgent (c) 2025 Vladislav Manoilov

!define APPNAME "WinBridgeAgent"
!define COMPANYNAME "Vladislav Manoilov"
!define VERSION "0.1.0" ; Should match service version ideally
!define DESCRIPTION "WinBridgeAgent Service - Secure File Access Bridge"
!define UNINSTALL_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}"
!define SERVICE_NAME "WinBridgeAgent"
!define SERVICE_EXE "WinBridgeAgent.exe"
!define CONTROL_PANEL_EXE "WinBridgeAgentControlPanel.exe"
!define INSTALL_DIR "$PROGRAMFILES64\${APPNAME}"

; --- Basic Settings ---
Name "${APPNAME} ${VERSION}"
OutFile "WinBridgeAgentInstaller.exe"
InstallDir "${INSTALL_DIR}"
InstallDirRegKey HKLM "${UNINSTALL_KEY}" "InstallLocation"
RequestExecutionLevel admin ; Request administrator privileges
ShowInstDetails show ; Show installation details (log)
ShowUnInstDetails show

; --- Modern UI --- (Optional, but good for a better look)
!include "MUI2.nsh"
!define MUI_ABORTWARNING ; Warn if user tries to abort
!define MUI_ICON "${NSISDIR}\Contrib\Graphics\Icons\modern-install.ico" ; Placeholder icon
!define MUI_UNICON "${NSISDIR}\Contrib\Graphics\Icons\modern-uninstall.ico" ; Placeholder icon

; --- Pages ---
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "LICENSE.txt" ; Path relative to the script location
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

; --- Language ---
!insertmacro MUI_LANGUAGE "English"

; --- Source Files Directory (Placeholder - to be set before compilation) ---
; This would be the path to the `dotnet publish` output for the service.
; For now, we assume files are in a `publish_output` subdirectory relative to this script.
!define SOURCE_FILES_DIR "publish_output"

; --- Installation Section ---
Section "Install ${APPNAME} Service" SEC_SERVICE
    SetOutPath "$INSTDIR"
    
    ; Package the WinBridgeAgent service files and Control Panel
    ; These files should come from the `dotnet publish` output of the WinBridgeAgent service project
    ; and the Control Panel project.
    File /r "${SOURCE_FILES_DIR}\*.*" ; Copy all files from the publish output (service and control panel)

    ; Write installation information to registry for uninstaller
    WriteRegStr HKLM "${UNINSTALL_KEY}" "DisplayName" "${APPNAME} (Service and Control Panel)"
    WriteRegStr HKLM "${UNINSTALL_KEY}" "DisplayVersion" "${VERSION}"
    WriteRegStr HKLM "${UNINSTALL_KEY}" "Publisher" "${COMPANYNAME}"
    StrCpy $R0 "\"$INSTDIR\\uninstall.exe\""
    WriteRegStr HKLM "${UNINSTALL_KEY}" "UninstallString" $R0
    WriteRegDWORD HKLM "${UNINSTALL_KEY}" "NoModify" 1
    WriteRegDWORD HKLM "${UNINSTALL_KEY}" "NoRepair" 1

    ; Create Uninstaller
    WriteUninstaller "$INSTDIR\uninstall.exe"

    ; Create Start Menu Shortcuts
    CreateDirectory "$SMPROGRAMS\${APPNAME}"
    CreateShortCut "$SMPROGRAMS\${APPNAME}\${APPNAME} Control Panel.lnk" "$INSTDIR\${CONTROL_PANEL_EXE}"
    CreateShortCut "$SMPROGRAMS\${APPNAME}\Uninstall ${APPNAME}.lnk" "$INSTDIR\uninstall.exe"

    ; Install and Start Service
    DetailPrint "Installing ${SERVICE_NAME} service..."
    ; Using nsExec to hide console window of sc.exe
    nsExec::ExecToLog 'sc.exe create "${SERVICE_NAME}" binPath= "$INSTDIR\${SERVICE_EXE}" start= auto DisplayName= "WinBridge Agent Service (Vlad)"'
    Pop $0 ; Get return code
    DetailPrint "sc.exe create exited with code $0"
    
    nsExec::ExecToLog 'sc.exe description "${SERVICE_NAME}" "${DESCRIPTION}"'
    Pop $0
    DetailPrint "sc.exe description exited with code $0"

    DetailPrint "Starting ${SERVICE_NAME} service..."
    nsExec::ExecToLog 'sc.exe start "${SERVICE_NAME}"'
    Pop $0
    DetailPrint "sc.exe start exited with code $0"
    
    ; If service start fails, it might be okay, user can start manually via Control Panel GUI
    ; Could add error checking here if strict start is required.

    DetailPrint "${APPNAME} Service installation complete."
SectionEnd

; --- Uninstallation Section ---
Section "Uninstall"
    DetailPrint "Stopping ${SERVICE_NAME} service..."
    nsExec::ExecToLog 'sc.exe stop "${SERVICE_NAME}"'
    Pop $0 ; Ignore error if service not running
    Sleep 2000 ; Give service time to stop

    DetailPrint "Deleting ${SERVICE_NAME} service..."
    nsExec::ExecToLog 'sc.exe delete "${SERVICE_NAME}"'
    Pop $0 ; Ignore error if service not found
    Sleep 1000

    DetailPrint "Removing files from $INSTDIR..."
    Delete "$INSTDIR\${SERVICE_EXE}"
    Delete "$INSTDIR\appsettings.json" ; And other known files
    Delete "$INSTDIR\uninstall.exe"
    ; Delete other DLLs and files - consider RMDir /r for the whole folder if safe
    RMDir /r "$INSTDIR\runtimes" ; Example for self-contained publish
    RMDir /r "$INSTDIR\BridgeLogs" ; Remove logs folder if it was created inside $INSTDIR
    RMDir "$INSTDIR" ; Remove the main directory if empty

    DeleteRegKey HKLM "${UNINSTALL_KEY}"

    ; Remove Start Menu Shortcuts
    Delete "$SMPROGRAMS\${APPNAME}\Uninstall ${APPNAME}.lnk"
    Delete "$SMPROGRAMS\${APPNAME}\${APPNAME} Control Panel.lnk"
    RMDir "$SMPROGRAMS\${APPNAME}"

    DetailPrint "${APPNAME} Service uninstallation complete."
SectionEnd

; --- Function to check if service exists (Optional, for more advanced logic) ---
Function IsServiceInstalled
    Push $R0
    ClearErrors
    ReadRegStr $R0 HKLM "SYSTEM\CurrentControlSet\Services\${SERVICE_NAME}" "DisplayName"
    IfErrors 0 ServiceFound
        StrCpy $R0 "0" ; Not Found
        Goto Done
    ServiceFound:
        StrCpy $R0 "1" ; Found
    Done:
    Exch $R0
FunctionEnd

