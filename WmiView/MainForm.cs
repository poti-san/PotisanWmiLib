using System.Diagnostics;
using System.Text;

using Potisan.Windows.Wmi;

namespace WmiView;

internal partial class MainForm : Form
{
	private sealed record class NamespaceClassInfo(string ClassName, string NameSpace, string Path, int MethodCount);

	private readonly WbemLocator _wmiLocator = new();
	private readonly List<NamespaceClassInfo> _nsClassInfos = [];
	private readonly List<NamespaceClassInfo> _nsClassInfosDisplay = [];

	public MainForm()
	{
		InitializeComponent();
	}

	private void AddNodes(TreeNode node, int depth = 0)
	{
		try
		{
			foreach (var ns in _wmiLocator.ConnectServer(node.FullPath).GetInstanceEnumerable("__NAMESPACE"))
			{
				var subNode = node.Nodes.Add(ns.Name);
				if (depth != 0)
				{
					AddNodes(subNode, depth > 0 ? depth - 1 : depth);
				}
				else
				{
					try
					{
						using var subNamespace = _wmiLocator.ConnectServer(subNode.FullPath).GetInstanceEnumerable("__NAMESPACE");
						if (subNamespace.Any())
						{
							subNode.Nodes.Add(new TreeNode() { Text = "dummy", Tag = "dummy" });
						}
					}
					catch (Exception ex)
					{
						subNode.ForeColor = Color.Gray;
						Debug.WriteLine(ex.Message);
					}
				}
			}
			node.Expand();
		}
		catch (Exception ex)
		{
			node.ForeColor = Color.Gray;
			Debug.WriteLine(ex.Message);
		}
	}

	private void MainForm_Load(object sender, EventArgs e)
	{
		const string msg = """
			�Ǘ��Ҍ����Ŏ��s���Ȃ��ꍇ�A���삪�x���Ȃ�A�擾�ł����񂪌������܂��B
			�{���Ɏ��s���܂����H
			""";
		if (!Program.IsAdmin && MessageBox.Show(msg, "WmiView", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
		{
			Close();
			return;
		}

		UpdateNamespaceClassInfos();
	}

	private void namespaceTreeView_AfterSelect(object sender, TreeViewEventArgs e)
	{
		UpdateNamespaceClassListView();
	}

	private void namespaceTreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
	{
		if (e.Node is not { } node || node.Nodes.Count != 1 || node.FirstNode!.Tag is not "dummy")
			return;

		UseWaitCursor = true;
		node.Nodes.Clear();
		AddNodes(node);
		UseWaitCursor = false;
	}

	private void searchSubNamespacesCheckBox_CheckedChanged(object sender, EventArgs e)
	{
		// �N���X���̍Ď擾
		UpdateNamespaceClassInfos();
	}

	private void hiddenSystemClassCheckBox_CheckedChanged(object sender, EventArgs e)
	{
		// �\�����̂ݍX�V
		UpdateNamespaceClassListView();
	}

	private void hasPropOnlyCheckBox_CheckedChanged(object sender, EventArgs e)
	{
		// �\�����̂ݍX�V
		UpdateNamespaceClassListView();
	}

	private void UpdateNamespaceClassInfos()
	{
		UseWaitCursor = true;
		Application.DoEvents();

		void SearchNodes(TreeNode node)
		{
			try
			{
				using var ns = _wmiLocator.ConnectServer(node.FullPath);
				foreach (var classInfo in ns.GetClassEnumerable())
				{
					_nsClassInfos.Add(new(
						classInfo.ClassName != null ? string.Intern(classInfo.ClassName) : "",
						classInfo.Namespace != null ? string.Intern(classInfo.Namespace) : "",
						classInfo.Path ?? "",
						classInfo.AllMethodNames.Length));
				}
			}
			catch
			{
				// TODO LOGGING
			}

			try
			{
				foreach (TreeNode subNode in node.Nodes)
				{
					if (subNode.Tag is not "dummy")
						SearchNodes(subNode);
				}
			}
			catch
			{
				// TODO LOGGING
			}
		}

		// �Â����̏����i�C���X�^���X�ꗗ�ƃ��\�b�h�ꗗ�j
		classListView.SelectedIndices.Clear();

		namespaceTreeView.BeginUpdate();
		namespaceTreeView.Nodes.Clear();
		_nsClassInfos.Clear();
		var root = namespaceTreeView.Nodes.Add("ROOT");

		// ���O��ԃc���[�r���[�̍X�V
		if (Program.IsAdmin)
		{
			AddNodes(root, -1);
		}
		else
		{
			// ROOT/CIMV2/SECURITY�ȉ��͊Ǘ��Ҍ������Ȃ��Ǝ~�܂�̂ŁA
			// �m�[�h��W�J���܂���B
			AddNodes(root, 1);
		}
		SearchNodes(root);

		namespaceTreeView.EndUpdate();

		namespaceTreeView.SelectedNode = root;

		UseWaitCursor = false;
	}

	private void UpdateNamespaceClassListView()
	{
		var classNameFilter = namespaceClassNameFilterTextBox.Text;
		var namespaceName = namespaceTreeView.SelectedNode!.FullPath;
		var hiddenSystemClass = hiddenSystemClassCheckBox.Checked;
		var hasPropsOnly = hasPropOnlyCheckBox.Checked;

		_nsClassInfosDisplay.Clear();
		_nsClassInfosDisplay.AddRange(_nsClassInfos
			.Where(info => info.NameSpace.StartsWith(namespaceName, StringComparison.OrdinalIgnoreCase)
				&& (!hiddenSystemClass || !info.ClassName.StartsWith("__"))
				&& (!hasPropsOnly || info.MethodCount != 0)
				&& (classNameFilter == null || info.ClassName.Contains(classNameFilter, StringComparison.OrdinalIgnoreCase))));

		// �~���ŕ��בւ��B
		_nsClassInfosDisplay.Sort((a, b) =>
		{
			var i = a.ClassName.CompareTo(b.ClassName);
			return i != 0 ? i : a.NameSpace.CompareTo(b.NameSpace);
		});

		classListView.VirtualListSize = _nsClassInfosDisplay.Count;
		classListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
		UseWaitCursor = false;
	}

	private void classListView_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
	{
		if (e.ItemIndex == -1) return;

		var info = _nsClassInfosDisplay[e.ItemIndex];
		var item = new ListViewItem([info.ClassName, info.NameSpace, info.MethodCount.ToString()]);

		e.Item = item;
	}

	private void namespaceClassNameFilterTextBox_TextChanged(object sender, EventArgs e)
	{
		UpdateNamespaceClassListView();
	}

	private void classListView_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (classListView.SelectedIndices.Count != 1)
		{
			instanceListView.Items.Clear();
			instanceListView.Columns.Clear(); // ����s�x�쐬
			methodListView.Items.Clear();
			tabPage1.Text = tabPage1.Tag as string ?? tabPage1.Text;
			tabPage2.Text = tabPage2.Tag as string ?? tabPage2.Text;
			return;
		}

		UseWaitCursor = true;
		Application.DoEvents();

		var classInfo = _nsClassInfosDisplay[classListView.SelectedIndices[0]];

		instanceListView.BeginUpdate();
		instanceListView.Items.Clear();
		instanceListView.Columns.Clear();

		using var ns = _wmiLocator.ConnectServer(classInfo.NameSpace);

		using var class_ = ns.GetObject(classInfo.Path);
		// �C���X�^���X�ꗗ�̍쐬
		{
			// ��̍쐬
			var propNames = class_.AllPropertyNames;
			instanceListView.Columns.AddRange([.. propNames.Select(s => new ColumnHeader { Text = s })]);

			// �C���X�^���X���̊��蓖��
			try
			{
				using var instances = ns.GetInstanceEnumerable(classInfo.ClassName, timeout: new(5000));
				foreach (var (i, instance) in instances.Index())
				{
					if (i == 1000)
					{
						if (TaskDialog.ShowDialog(this, new()
						{
							Text = "�������ʂ�1000�����z���܂����B�������p�����܂����H�i�ȍ~�͍Ō�܂Ō������܂��j",
							Icon = TaskDialogIcon.Information,
							Buttons = [TaskDialogButton.Yes, TaskDialogButton.No],
						}
						) != TaskDialogButton.Yes)
						{
							break;
						}
					}

					instanceListView.Items.Add(new ListViewItem([.. propNames
						.Select(s => instance[s].Value switch
						{
							IEnumerable<object> enum_ => string.Join(", ", enum_),
							{ } v => v.ToString() ?? "",
							_ => "",
						})]));
				}
			}
			catch
			{
				// �C���X�^���X�����݂��Ȃ��ꍇ��
			}

			instanceListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
			instanceListView.EndUpdate();

			// �^�u�Ɍ��̒ǉ�
			tabPage1.Tag ??= tabPage1.Text;
			tabPage1.Text = $"{tabPage1.Tag} ({instanceListView.Items.Count})";
		}
		// ���\�b�h�ꗗ�̍쐬
		{
			methodListView.BeginUpdate();
			methodListView.Items.Clear();
			foreach (var (name, inSig, outSig) in class_.EnumerateMethodInfos())
			{
				methodListView.Items.Add(new ListViewItem([
					name,
					inSig?.NonSystemPropertyNames is { } inArgs ? string.Join(", ", inArgs) : "",
					outSig?.NonSystemPropertyNames is { } outArgs ? string.Join(", ", outArgs) : "",
				]));
			}
			methodListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
			methodListView.EndUpdate();

			// �^�u�Ɍ��̒ǉ�
			tabPage2.Tag ??= tabPage2.Text;
			tabPage2.Text = $"{tabPage2.Tag} ({methodListView.Items.Count})";
		}

		UseWaitCursor = false;
	}

	private void toolCloseToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Close();
	}

	private void toolUpdateMenuItem_Click(object sender, EventArgs e)
	{
		UpdateNamespaceClassInfos();
	}

	private void toolCopyNamespaceNameMenuItem_Click(object sender, EventArgs e)
	{
		if (namespaceTreeView.SelectedNode is not { } node)
			return;
		Clipboard.Clear();
		Clipboard.SetText(node.Text);
	}

	private void toolCopyNamespacePathMenuItem_Click(object sender, EventArgs e)
	{
		if (namespaceTreeView.SelectedNode is not { } node)
			return;
		Clipboard.Clear();
		Clipboard.SetText(node.FullPath);
	}

	private void toolSaveClassListToCsvFileMenuItem_Click(object sender, EventArgs e)
	{
		if (namespaceTreeView.SelectedNode is not { } node)
		{
			TaskDialog.ShowDialog(this, new() { Text = "���O��Ԃ����I���ł��B", Icon = TaskDialogIcon.Error });
			return;
		}
		csvSaveFileDialog.FileName = $"{node.FullPath.Replace('\\', '_')}.csv";

		if (csvSaveFileDialog.ShowDialog(this) != DialogResult.OK)
			return;
		var path = csvSaveFileDialog.FileName;

		using var f = File.OpenWrite(path);
		var writer = new StreamWriter(f, Encoding.UTF8);
		writer.WriteLine("���O,���O���,���\�b�h��");
		foreach (var item in _nsClassInfosDisplay)
		{
			writer.WriteLine($"{item.ClassName},{item.NameSpace}");
		}
		f.Flush(true);
	}

	private void toolSaveInstanceListToCsvFileMenuItem_Click(object sender, EventArgs e)
	{
		// ���X�g�r���[�̗�ƍ��ڂ����̂܂܏o�͂��܂��B
		if (classListView.SelectedIndices.Count != 1)
		{
			TaskDialog.ShowDialog(this, new() { Text = "�N���X�����I���������I������Ă��܂��B", Icon = TaskDialogIcon.Error });
			return;
		}
		var classInfo = _nsClassInfosDisplay[classListView.SelectedIndices[0]];

		csvSaveFileDialog.FileName = $"{classInfo.ClassName} ({classInfo.NameSpace.Replace('\\', '_')}).csv";
		if (csvSaveFileDialog.ShowDialog(this) != DialogResult.OK)
			return;
		var path = csvSaveFileDialog.FileName;

		using var f = File.OpenWrite(path);
		var writer = new StreamWriter(f, Encoding.UTF8);

		// �񖼂̏����o��
		// �^�C�v�~�X�m�F�̂��߂�OfType�ł͂Ȃ�Cast���g���܂��B
		writer.WriteLine(string.Join(",",
			instanceListView.Columns.Cast<ColumnHeader>().Select(column => $"\"{column.Text}\"")));

		// �A�C�e���̏����o��
		foreach (ListViewItem item in instanceListView.Items)
		{
			var s = string.Join(",", item.SubItems.Cast<ListViewItem.ListViewSubItem>().Select(item => $"\"{item.Text.Replace("\"", "\"\"")}\""));
			writer.WriteLine(s);
		}

		f.Flush(true);
	}
}
