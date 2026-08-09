[Setup]
; Уникальный идентификатор приложения (GUID)
AppId={{B5832F31-9A1B-4C2E-8F1A-73C8E4F1B3E2}
AppName=Syrlas Studio
AppVersion=1.0
AppPublisher=Syrlas AI
AppPublisherURL=https://github.com/
DefaultDirName={autopf}\SyrlasStudio
DefaultGroupName=Syrlas Studio
AllowNoIcons=yes
; Папка, куда сохранится готовый установщик
OutputDir=C:\Users\alexs\SyrlasStudio\InstallerOutput
OutputBaseFilename=SyrlasStudio_Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на &Рабочем столе"; GroupDescription: "Дополнительные значки:"; Flags: unchecked

[Files]
; Упаковываем всю папку publish со всеми скомпилированными файлами, DLL и рантаймами .NET
Source: "C:\Users\alexs\SyrlasStudio\SyrlasStudio\SyrlasStudio\bin\Release\net9.0-windows10.0.19041.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; (Опционально) Если хотите, чтобы файл весов модели автоматически укладывался в папку приложения при установке:
 Source: "C:\Users\alexs\SyrlasStudio\SyrlasAIEngine\Model\Qwen2.5-1.5B-Instruct-Q4_K_L.gguf"; DestDir: "{app}\Model"; Flags: ignoreversion

[Icons]
Name: "{group}\Syrlas Studio"; Filename: "{app}\SyrlasStudio.exe"
Name: "{group}\Удалить Syrlas Studio"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Syrlas Studio"; Filename: "{app}\SyrlasStudio.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\SyrlasStudio.exe"; Description: "Запустить Syrlas Studio"; Flags: nowait postinstall skipifsilent