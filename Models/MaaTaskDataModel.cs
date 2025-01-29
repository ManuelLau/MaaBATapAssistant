namespace MaaBATapAssistant.Models;

//用于Maa任务的数据模型
public class MaaTaskDataModel
{
    public string Name {  get; set; }
    public string Entry {  get; set; }
    public string PipelineOverride { get; set; }
    public string StartLogMessage { get; set; }
    public string SucceededLogMessage { get; set; }
    public string FailedLogMessage { get; set; }

    public MaaTaskDataModel(string _name, string _entry, string _pipelineOverride, 
        string _startLogMessage, string _succeededLogMessage, string _failedLogMessage)
    {
        Name = _name;
        Entry = _entry;
        PipelineOverride = _pipelineOverride;
        StartLogMessage = _startLogMessage;
        SucceededLogMessage = _succeededLogMessage;
        FailedLogMessage = _failedLogMessage;
    }
}
