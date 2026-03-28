using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaaBATapAssistant.Models;
using MaaBATapAssistant.Utils;

namespace MaaBATapAssistant.ViewModels;

public partial class UpdateViewModel : ObservableObject
{
    [ObservableProperty]
    public ProgramDataModel programData = ProgramDataModel.Instance;
    private bool appCanUpdate = false;
    private string tempFileName = string.Empty;

    public UpdateViewModel()
    {
        if (!ProgramData.IsCheckingNewVersion && !ProgramData.IsDownloadingFiles && !ProgramData.IsReadyForApplyUpdate)
        {
            _ = CheckNewVersion();
        }
    }

    [RelayCommand]
    private async Task CheckNewVersion()
    {
        await Task.Run(() =>
        {
            UpdateTool.CheckNewVersion(false, out _, out appCanUpdate);
            if (appCanUpdate)
            {
                ProgramData.HasNewVersion = true;
            }
            else
            {
                ProgramData.HasNewVersion = false;
            }
        });
    }

    [RelayCommand]
    public async Task DownloadUpdateButtonClick()
    {
        if (appCanUpdate)
        {
            tempFileName = await UpdateTool.UpdateApp();
        }
    }

    [RelayCommand]
    public void ApplyUpdateButtonClick()
    {
        UpdateTool.ApplyUpdate(tempFileName);
    }
}
