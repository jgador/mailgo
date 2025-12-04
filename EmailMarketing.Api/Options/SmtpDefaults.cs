namespace EmailMarketing.Api.Options;

public class SmtpDefaults
{
    public const string SectionName = "Smtp";

    public string? DefaultHost { get; set; }
    public int? DefaultPort { get; set; }
    public bool? DefaultUseSsl { get; set; }
    public bool? DefaultUseStartTls { get; set; }
}
