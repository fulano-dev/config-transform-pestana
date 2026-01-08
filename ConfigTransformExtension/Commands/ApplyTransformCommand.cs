using System;
using System.ComponentModel.Design;
using System.IO;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using Task = System.Threading.Tasks.Task;

namespace ConfigTransformExtension.Commands
{
    internal sealed class ApplyTransformCommand
    {
        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d");

        private readonly AsyncPackage package;
        private readonly Services.XmlTransformService transformService;

        private ApplyTransformCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            transformService = new Services.XmlTransformService();

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new OleMenuCommand(this.Execute, menuCommandID);
            menuItem.BeforeQueryStatus += MenuItem_BeforeQueryStatus;
            commandService.AddCommand(menuItem);
        }

        public static ApplyTransformCommand Instance { get; private set; }

        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get { return this.package; }
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new ApplyTransformCommand(package, commandService);
        }

        private void MenuItem_BeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null) return;

            menuCommand.Visible = false;
            menuCommand.Enabled = false;

            string filePath = GetSelectedFilePath();
            if (!string.IsNullOrEmpty(filePath))
            {
                string fileName = Path.GetFileName(filePath).ToLower();
                
                // Mostra o comando apenas para arquivos .config que não sejam o base
                if (fileName.EndsWith(".config") && 
                    fileName != "web.config" && 
                    fileName != "app.config" &&
                    (fileName.Contains("pestana") || fileName.Contains(".") && fileName.Split('.').Length > 2))
                {
                    menuCommand.Visible = true;
                    menuCommand.Enabled = true;
                }
            }
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                string transformFilePath = GetSelectedFilePath();
                if (string.IsNullOrEmpty(transformFilePath))
                {
                    ShowMessage("Nenhum arquivo selecionado.", OLEMSGICON.OLEMSGICON_WARNING);
                    return;
                }

                string baseConfigFile = transformService.FindBaseConfigFile(transformFilePath);
                if (string.IsNullOrEmpty(baseConfigFile))
                {
                    ShowMessage($"Arquivo base (web.config ou app.config) não encontrado no mesmo diretório do arquivo:\n{transformFilePath}", 
                        OLEMSGICON.OLEMSGICON_WARNING);
                    return;
                }

                string environmentName = transformService.GetEnvironmentName(transformFilePath);
                
                var result = MessageBox.Show(
                    $"Deseja aplicar a transformação do ambiente {environmentName}?\n\n" +
                    $"Arquivo base: {Path.GetFileName(baseConfigFile)}\n" +
                    $"Transformação: {Path.GetFileName(transformFilePath)}\n\n" +
                    $"ATENÇÃO: O arquivo {Path.GetFileName(baseConfigFile)} será SOBRESCRITO!",
                    "Confirmar Aplicação de Transformação",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);

                if (result != DialogResult.Yes)
                {
                    return;
                }

                // Cria backup antes de aplicar
                string backupFile = baseConfigFile + ".backup";
                File.Copy(baseConfigFile, backupFile, true);

                // Aplica a transformação
                bool success = transformService.ApplyTransform(baseConfigFile, transformFilePath, baseConfigFile);

                if (success)
                {
                    ShowMessage(
                        $"Transformação aplicada com sucesso!\n\n" +
                        $"Ambiente: {environmentName}\n" +
                        $"Arquivo atualizado: {Path.GetFileName(baseConfigFile)}\n\n" +
                        $"Backup salvo em: {Path.GetFileName(backupFile)}",
                        OLEMSGICON.OLEMSGICON_INFO);

                    // Recarrega o arquivo no editor se estiver aberto
                    ReloadFileInEditor(baseConfigFile);
                }
                else
                {
                    // Restaura o backup em caso de falha
                    File.Copy(backupFile, baseConfigFile, true);
                    ShowMessage("Falha ao aplicar transformação. O arquivo foi restaurado do backup.", 
                        OLEMSGICON.OLEMSGICON_CRITICAL);
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Erro ao aplicar transformação:\n\n{ex.Message}", OLEMSGICON.OLEMSGICON_CRITICAL);
            }
        }

        private string GetSelectedFilePath()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            IntPtr hierarchyPtr, selectionContainerPtr;
            uint itemId;
            IVsMultiItemSelect multiItemSelect;
            
            IVsMonitorSelection monitorSelection = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            if (monitorSelection == null) return null;

            monitorSelection.GetCurrentSelection(out hierarchyPtr, out itemId, out multiItemSelect, out selectionContainerPtr);

            if (itemId != VSConstants.VSITEMID_NIL && hierarchyPtr != IntPtr.Zero)
            {
                IVsHierarchy hierarchy = System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(hierarchyPtr) as IVsHierarchy;
                if (hierarchy != null)
                {
                    string itemPath;
                    hierarchy.GetCanonicalName(itemId, out itemPath);
                    return itemPath;
                }
            }

            return null;
        }

        private void ReloadFileInEditor(string filePath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                if (dte != null)
                {
                    // Procura se o arquivo está aberto
                    foreach (EnvDTE.Window window in dte.Windows)
                    {
                        if (window.Document != null && 
                            string.Equals(window.Document.FullName, filePath, StringComparison.OrdinalIgnoreCase))
                        {
                            window.Document.Close(EnvDTE.vsSaveChanges.vsSaveChangesNo);
                            dte.ItemOperations.OpenFile(filePath);
                            break;
                        }
                    }
                }
            }
            catch
            {
                // Ignora erros ao recarregar - não é crítico
            }
        }

        private void ShowMessage(string message, OLEMSGICON icon)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            VsShellUtilities.ShowMessageBox(
                this.package,
                message,
                "Config Transform Extension",
                icon,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
