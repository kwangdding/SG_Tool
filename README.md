# vsCode 에서 프로젝트 생성 명령어
- 원하는 경로에가서 생성할 프로젝트 이름으로 생성
    dotnet new winforms -o SEND_EMAIL


# 빌드 명령어
cd SEND_EMAIL
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true



# dotnet add package SSH.NET
dotnet add package Microsoft.Office.Interop.Outlook
dotnet add package Microsoft.NET.Sdk.WindowsDesktop
