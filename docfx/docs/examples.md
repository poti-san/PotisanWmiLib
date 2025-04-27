# サンプルコード

## Win32_\*クラス (Win32_Perf\*クラスを除く)の名前を取得

```cs
using Potisan.Windows.Wmi;

// WMI用にCOMセキュリティを初期化
WmiUtility.InitializeComSecurity();

using var locator = new WbemLocator();
using var ns = locator.ConnectServer(@"root\cimv2");

var classNames = ns.GetClassEnumerable().Select(info => info.ClassName)
	.Where(s => s.StartsWith("Win32_", StringComparison.OrdinalIgnoreCase))
	.Where(s => !s.StartsWith("Win32_Perf", StringComparison.OrdinalIgnoreCase))
	.Distinct(StringComparer.OrdinalIgnoreCase)
	.Order()
	.ToArray();

foreach (var className in classNames)
	Console.WriteLine(className);
```

## OS情報の取得

```cs
using Potisan.Windows.Wmi;

// WMI用にCOMセキュリティを初期化
WmiUtility.InitializeComSecurity();

using var locator = new WbemLocator();
using var ns = locator.ConnectServer(@"root\cimv2");

foreach (var (i, os) in ns.GetInstanceEnumerable("Win32_OperatingSystem").Index())
{
	Console.WriteLine($"""
		Operating System #{i}
		  Caption: {os["Caption"].Value}
		  Manufacturer: {os["Manufacturer"].Value}
		  Organization: {os["Organization"].Value}
		  SystemDevice: {os["SystemDevice"].Value}
		""");
}
```

## コンピューター情報の取得

```cs
using Potisan.Windows.Wmi;

// WMI用にCOMセキュリティを初期化
WmiUtility.InitializeComSecurity();

using var locator = new WbemLocator();
using var ns = locator.ConnectServer(@"root\cimv2");

var computerSystem = ns.GetInstanceEnumerable("Win32_ComputerSystem").First();
Console.WriteLine($"""
	製造者:         {computerSystem["Manufacturer"].Value}
	モデル:         {computerSystem["Model"].Value}
	システムタイプ: {computerSystem["SystemType"].Value}
	総物理メモリ:   {computerSystem["TotalPhysicalMemory"].Value}
	""");
```

## 適当なインスタンスの同期処理取得

```cs
using System.Diagnostics;

using Potisan.Windows.Wmi;

// WMI用にCOMセキュリティを初期化
WmiUtility.InitializeComSecurity();

using var locator = new WbemLocator();
using var ns = locator.ConnectServer(@"root\cimv2");
using var obj = ns.GetInstanceEnumerable("Win32_OperatingSystem").First();

using var unsecuredApartment = new WbemUnsecuredApartment();

var objectSink1 = unsecuredApartment.CreateSinkStub(WbemUnsecuredApartmentCheckAccess.DefaultCheckAccess,
	objects => Debug.WriteLine($"Indicate({string.Join(", ", objects.Select(obj => obj.ClassName))})"),
	(status, hresult, strParam, objParam) => Debug.WriteLine($"SetStatus({status}, {hresult}, {strParam}, {objParam})"));

ns.GetObjectAsync(objectSink1, obj.Path!);

Thread.Sleep(1000); // 同期処理を待機
```
