using Mnemo.Contracts.Pack.Requests;
using Mnemo.Data.Entities;
using Mnemo.Shared.Enums;
using System.Runtime.CompilerServices;

namespace Mnemo.Shared.Extensions
{
    public static class VocabularyPackExstension
    {
        public static bool TryPatch(this VocabularyPack pack, PatchPackRequest patch)
        {
            string? _name = null;
            if (patch.Name != null)
            {
                var normalized = pack.Name.RemoveMultispaces().Capitalize();
                if (string.IsNullOrWhiteSpace(normalized))
                    return false;

                _name = normalized;
            }

            string? _description = null;
            if (patch.Description != null)
            {
                var normalized = pack.Description.RemoveMultispaces().Capitalize();
                if (string.IsNullOrWhiteSpace(_description))
                    return false;

                _description = normalized;
            }

            Visibility? _visibility = null;
            if (patch.Visibility != null)
            {
                if (!Enum.TryParse<Visibility>(patch.Visibility, true, out var parsedVisibility))
                    return false;

                _visibility = parsedVisibility;
            }


            if (_name != null)
                pack.Name = _name;
            if (_description != null)
                pack.Description = _description;
            if (_visibility != null)
                pack.Visibility = _visibility.Value;

            return true;
        }
    }
}
