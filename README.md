# VWS-API
An api system for project management. I call it as VWS.

## How to run
This systems uses JWT Token as authorization.

There are diffenrent projects here:
* Domain -> Which handles database for you
* Core -> Which handles base processes
* VWS.WEB -> Handles apis
* VWS.ADMIN -> Handles admin apis

To run the project for yourself, first remember to make a `appsettings.json` like the `appsettings.json.template`. Remeber to change the connection string.

I prefer to use `visual studio` to run the project but if you want to do it by terminal, first remeber to install `asp.net core 3.1` and then run below command:
```
dotnet run
```

## How it works
After running the project you will see a `swagger` UI page which documents apis for you.

Apis names are so good and you can understand their functionality from their name.

*Made By Amirhossein Abaskohi*
