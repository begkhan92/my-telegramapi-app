namespace bnmini_crm.Services;

public class ManagerAccessService
{
    private readonly Dictionary<string, (int VenueId, DateTime Expires)> _codes = new();

    public string GenerateCode(int venueId)
    {
        var code = Guid.NewGuid().ToString("N");
        _codes[code] = (venueId, DateTime.UtcNow.AddMinutes(5));
        return code;
    }

    public int? ValidateCode(string code)
    {
        if (_codes.TryGetValue(code, out var entry))
        {
            if (entry.Expires > DateTime.UtcNow)
            {
                _codes.Remove(code);
                return entry.VenueId;
            }
            _codes.Remove(code);
        }
        return null;
    }
}