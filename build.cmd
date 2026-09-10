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

if not exist "firawselector.ico" (
    echo [0/4] icone...
    "%CSC%" /nologo /target:exe /codepage:65001 /r:System.dll /r:System.Drawing.dll /out:"tools\mkico.exe" "tools\mkico.cs"
    if errorlevel 1 exit /b 1
    "tools\mkico.exe" "firawselector.ico"
    if errorlevel 1 exit /b 1
)

echo [1/4] motor...
"%CSC%" %OPTS% %REFS% /win32icon:"firawselector.ico" /out:"FirawSelector.exe" FirawSelector.cs %COMUM%
if errorlevel 1 exit /b 1

echo [2/4] configurador...
"%CSC%" %OPTS% %REFS% /win32icon:"firawselector.ico" /out:"FirawSelector Studio.exe" FirawSelectorStudio.cs %COMUM%
if errorlevel 1 exit /b 1

echo [3/4] instalador...
"%CSC%" %OPTS% %REFS% /win32icon:"firawselector.ico" /resource:"FirawSelector.exe" /resource:"FirawSelector Studio.exe" /out:"FirawSelector Setup.exe" FirawSelectorSetup.cs %COMUM%
if errorlevel 1 exit /b 1

echo [4/4] copiando para dist...
if not exist dist mkdir dist
copy /y "FirawSelector.exe" "dist\FirawSelector.exe" >nul
copy /y "FirawSelector Studio.exe" "dist\FirawSelector-Studio.exe" >nul
copy /y "FirawSelector Setup.exe" "dist\FirawSelector-Setup.exe" >nul

echo.
echo Pronto. Rode "FirawSelector Setup.exe" para instalar.
endlocal
