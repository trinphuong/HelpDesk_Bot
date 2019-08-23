nuget restore
msbuild QnABot.sln -p:DeployOnBuild=true -p:PublishProfile=noc-bot-qna-service-Web-Deploy.pubxml -p:Password=Ax3js71PjQQSrnTfCCtfHGDE0Aa40wwgdsLrvoAvLMnyReWvfFD9zvffKgqX

