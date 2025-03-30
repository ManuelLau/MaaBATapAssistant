using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaaBATapAssistant.Models;
using MaaBATapAssistant.Utils;

namespace MaaBATapAssistant.ViewModels;

public partial class UpdateViewModel : ObservableObject
{
    [ObservableProperty]
    public ProgramDataModel programData = ProgramDataModel.Instance;
    [ObservableProperty]
    public string text = string.Empty;
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
            if (UpdateTool.CheckNewVersion(false))
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
        tempFileName = await UpdateTool.UpdateApp();
    }

    [RelayCommand]
    public void ApplyUpdateButtonClick()
    {
        UpdateTool.ApplyUpdate(tempFileName);
    }
}
