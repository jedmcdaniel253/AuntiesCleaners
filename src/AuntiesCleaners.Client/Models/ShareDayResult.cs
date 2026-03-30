namespace AuntiesCleaners.Client.Models;

public enum ShareMode { Workers, Owner }

public record ShareDayResult(ShareMode Mode, Owner? SelectedOwner);
