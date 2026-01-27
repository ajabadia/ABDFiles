using System;

namespace GeneradorCartas.Services;

public interface IGenerationProgress
{
    void ReportProgress(int current, int total, string message);
    void ReportLog(string message);
}
