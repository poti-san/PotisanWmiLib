using System.Security.Principal;

using Potisan.Windows.Wmi;

namespace WmiView;

internal static class Program
{
	public static bool IsAdmin { get; private set; }

	[STAThread]
	static void Main()
	{
		// WMI�p��COM�Z�L�����e�B�����������܂��B
		WmiUtility.InitializeComSecurity();

		ApplicationConfiguration.Initialize();

		// �Ǘ��Ҍ����̊m�F
		using (var identify = WindowsIdentity.GetCurrent())
		{
			var principal = new WindowsPrincipal(identify);
			IsAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
		}

		ToolStripManager.Renderer = new ToolStripSystemRenderer();
		Application.Run(new MainForm());
	}
}