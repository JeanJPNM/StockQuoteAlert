# Stock Quote Alert

A simple C# console application that monitors brazillian stock prices in fixed intervals,
notifying you when they fall bellow/rise above certain thresholds.


## Usage

This project required .NET 8 or above.

First you must configure the application, run the following command on the project's directory and enter the requested values. The configuration file will be placed in that same directory.

```sh
dotnet run -- configure
```

After that, you can run the watch command to monitor a stock's price.

```sh
dotnet run -- watch PETR4 22.67 22.59
```