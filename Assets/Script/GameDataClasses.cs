using System.Collections.Generic;

[System.Serializable]
public class LevelConfig
{
    public int levelID;
    public int minVal;
    public int maxVal;
    public string[] allowOps;
}

[System.Serializable]
public class ModeConfig
{
    public string modeName;
    public List<LevelConfig> levels;
}

[System.Serializable]
public class GradeConfig
{
    public int gradeID;
    public List<ModeConfig> gameModes;
}

[System.Serializable]
public class GameDataContainer
{
    public List<GradeConfig> grades;
}