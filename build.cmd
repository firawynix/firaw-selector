@echo off
setlocal

rem Compila o FirawSelector com o compilador C# que ja vem no Windows.
rem Nao precisa de Visual Studio, SDK nem nada baixado.

set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist "%CSC%" (
    echo Nao achei o csc.exe do .NET Framework 4.x.
    exit /b 1
)

set REFS=/r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll
rem /codepage:65001 - os textos em portugues sao UTF-8; sem isso o csc le pela
rem pagina de codigo do sistema e os acentos chegam trocados na tela.
set OPTS=/nologo /target:winexe /platform:anycpu /codepage:65001 /optimize+
set COMUM=Core.cs Modelo.cs Janela.cs

cd /d "%~dp0"

rem A versao mora em UM lugar so (Amb.Versao, no Core.cs). Este trecho a copia
rem para os atributos do assembly — sem isso o Windows mostra 0.0.0.0 nas
rem propriedades do arquivo e nao da para saber qual build esta instalado.
set VER=
for /f "tokens=2 delims==" %%v in ('findstr /c:"public const string Versao" Core.cs') do set VER=%%v
set VER=%VER: =%
set VER=%VER:"=%
set VER=%VER:;=%
if "%VER%"=="" (
    echo Nao consegui ler a versao do Core.cs.
    exit /b 1
)
> "VersaoInfo.cs" echo // Gerado por build.cmd a partir de Amb.Versao. Nao edite.
>>"VersaoInfo.cs" echo using System.Reflection;
>>"VersaoInfo.cs" echo [assembly: AssemblyTitle("FirawSelector")]
>>"VersaoInfo.cs" echo [assembly: AssemblyProduct("FirawSelector")]
>>"VersaoInfo.cs" echo [assembly: AssemblyCompany("Firawynix")]
>>"VersaoInfo.cs" echo [assembly: AssemblyVersion("%VER%.0")]
>>"VersaoInfo.cs" echo [assembly: AssemblyFileVersion("%VER%.0")]
set COMUM=%COMUM% VersaoInfo.cs

echo [0/5] icone...
"%CSC%" /nologo /target:exe /codepage:65001 /r:System.dll /r:System.Drawing.dll /out:"tools\mkico.exe" "tools\mkico.cs"
if errorlevel 1 exit /b 1
"tools\mkico.exe" "firawselector.ico"
if errorlevel 1 exit /b 1

echo [1/5] motor...
"%CSC%" %OPTS% %REFS% /win32icon:"firawselector.ico" /out:"FirawSelector.exe" FirawSelector.cs %COMUM%
if errorlevel 1 exit /b 1
call :sign "FirawSelector.exe"
if errorlevel 1 exit /b 1

echo [2/5] ponte das extensoes...
"%CSC%" /nologo /target:exe /platform:anycpu /codepage:65001 /optimize+ /win32icon:"firawselector.ico" /r:System.dll /r:System.Web.Extensions.dll /out:"FirawSelector Host.exe" FirawSelectorHost.cs Core.cs VersaoInfo.cs /main:ProgramaHost
if errorlevel 1 exit /b 1
call :sign "FirawSelector Host.exe"
if errorlevel 1 exit /b 1

echo [3/5] configurador...
"%CSC%" %OPTS% %REFS% /win32icon:"firawselector.ico" /out:"FirawSelector Studio.exe" FirawSelectorStudio.cs %COMUM%
if errorlevel 1 exit /b 1
call :sign "FirawSelector Studio.exe"
if errorlevel 1 exit /b 1

echo [4/5] instalador...
"%CSC%" %OPTS% %REFS% /win32icon:"firawselector.ico" /resource:"FirawSelector.exe" /resource:"FirawSelector Studio.exe" /resource:"FirawSelector Host.exe" /resource:"extensions\chrome\manifest.json",ext.chrome.manifest.json /resource:"extensions\edge\manifest.json",ext.edge.manifest.json /resource:"extensions\firefox\manifest.json",ext.firefox.manifest.json /resource:"extensions\shared\background.js",ext.background.js /resource:"extensions\shared\content.js",ext.content.js /resource:"extensions\shared\icon16.png",ext.icon16.png /resource:"extensions\shared\icon32.png",ext.icon32.png /resource:"extensions\shared\icon48.png",ext.icon48.png /resource:"extensions\shared\icon128.png",ext.icon128.png /out:"FirawSelector Setup.exe" FirawSelectorSetup.cs %COMUM%
if errorlevel 1 exit /b 1
call :sign "FirawSelector Setup.exe"
if errorlevel 1 exit /b 1

echo [5/5] copiando para dist...
if not exist dist mkdir dist
copy /y "FirawSelector.exe" "dist\FirawSelector.exe" >nul
copy /y "FirawSelector Host.exe" "dist\FirawSelector-Host.exe" >nul
copy /y "FirawSelector Studio.exe" "dist\FirawSelector-Studio.exe" >nul
copy /y "FirawSelector Setup.exe" "dist\FirawSelector-Setup.exe" >nul
copy /y "UPDATE.md" "dist\UPDATE.md" >nul

if exist "dist\extensions" rmdir /s /q "dist\extensions"
for %%b in (chrome edge firefox) do (
    mkdir "dist\extensions\%%b"
    copy /y "extensions\%%b\manifest.json" "dist\extensions\%%b\manifest.json" >nul
    copy /y "extensions\shared\background.js" "dist\extensions\%%b\background.js" >nul
    copy /y "extensions\shared\content.js" "dist\extensions\%%b\content.js" >nul
    copy /y "extensions\shared\icon*.png" "dist\extensions\%%b\" >nul
)
for %%b in (Chrome Edge Firefox) do if exist "dist\FirawSelector-%%b.zip" del /q "dist\FirawSelector-%%b.zip"
powershell -NoProfile -Command "Compress-Archive -Path 'dist\extensions\chrome\*' -DestinationPath 'dist\FirawSelector-Chrome.zip'"
if errorlevel 1 exit /b 1
powershell -NoProfile -Command "Compress-Archive -Path 'dist\extensions\edge\*' -DestinationPath 'dist\FirawSelector-Edge.zip'"
if errorlevel 1 exit /b 1
powershell -NoProfile -Command "Compress-Archive -Path 'dist\extensions\firefox\*' -DestinationPath 'dist\FirawSelector-Firefox.zip'"
if errorlevel 1 exit /b 1

powershell -NoProfile -Command "$names=@('FirawSelector-Setup.exe','FirawSelector.exe','FirawSelector-Studio.exe','FirawSelector-Host.exe','FirawSelector-Chrome.zip','FirawSelector-Edge.zip','FirawSelector-Firefox.zip'); $sha=[Security.Cryptography.SHA256]::Create(); $lines=foreach($name in $names){$stream=[IO.File]::OpenRead((Join-Path $PWD ('dist\'+$name))); try{$bytes=$sha.ComputeHash($stream)}finally{$stream.Dispose()}; ([BitConverter]::ToString($bytes)).Replace('-','').ToLowerInvariant()+'  '+$name}; $sha.Dispose(); [IO.File]::WriteAllLines((Join-Path $PWD 'dist\SHA256SUMS.txt'),$lines,[Text.Encoding]::ASCII); [IO.File]::WriteAllText((Join-Path $PWD 'dist\FirawSelector-Setup.exe.sha256'),$lines[0]+[Environment]::NewLine,[Text.Encoding]::ASCII)"
if errorlevel 1 exit /b 1

echo.
echo Pronto. Rode "FirawSelector Setup.exe" para instalar.
exit /b 0

:sign
if not defined FIRAW_SIGNING_THUMBPRINT exit /b 0
if not defined FIRAW_SIGNTOOL (
    echo FIRAW_SIGNTOOL nao foi informado para assinar %~1.
    exit /b 1
)
"%FIRAW_SIGNTOOL%" sign /fd sha256 /sha1 "%FIRAW_SIGNING_THUMBPRINT%" /d "FirawSelector" /tr http://timestamp.digicert.com /td sha256 %1
exit /b %ERRORLEVEL%
