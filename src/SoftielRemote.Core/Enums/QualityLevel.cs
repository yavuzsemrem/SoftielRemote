namespace SoftielRemote.Core.Enums;

/// <summary>
/// Ekran paylaşımı kalite seviyesini temsil eder.
/// </summary>
public enum QualityLevel
{
    /// <summary>
    /// Düşük kalite - Daha az bant genişliği, daha düşük çözünürlük.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Orta kalite - Dengeli bant genişliği ve kalite.
    /// </summary>
    Medium = 1,

    /// <summary>
    /// Yüksek kalite - Yüksek çözünürlük, daha fazla bant genişliği.
    /// </summary>
    High = 2,

    /// <summary>
    /// Otomatik kalite - Ağ durumuna göre otomatik ayarlanır.
    /// </summary>
    Auto = 3
}

