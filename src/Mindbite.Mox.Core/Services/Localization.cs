using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Mindbite.Mox.Services
{
    public class LocalizationSources
    {
        public List<Type> ResouceTypes { get; set; } = new List<Type>();
    }

    public class MoxStringLocalizer : IStringLocalizer
    {
        private readonly LocalizationSources _sources;
        private readonly IStringLocalizerFactory _localizerFactory;

        public MoxStringLocalizer(IOptions<LocalizationSources> sources, IStringLocalizerFactory localizerFactory)
        {
            this._sources = sources.Value;
            this._localizerFactory = localizerFactory;
        }

        public virtual LocalizedString this[string name] => this[name, arguments: new object[0]];

        public virtual LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                for (int i = this._sources.ResouceTypes.Count - 1; i >= 0; i--)
                {
                    var resourceType = this._sources.ResouceTypes[i];
                    var found = this._localizerFactory.Create(resourceType)[name, arguments];
                    if (!found.ResourceNotFound)
                        return found;
                }

                return new LocalizedString(name, string.Format(name, arguments));
            }
        }

        public virtual IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            return this._sources.ResouceTypes.SelectMany(x => this._localizerFactory.Create(x).GetAllStrings());
        }

        public virtual IStringLocalizer WithCulture(CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MoxDataAnnotationsLocalizationOptionsSetup : IConfigureOptions<MvcDataAnnotationsLocalizationOptions>
    {
        private readonly IOptions<LocalizationSources> _sources;
        public MoxDataAnnotationsLocalizationOptionsSetup(IOptions<LocalizationSources> sources)
        {
            this._sources = sources;
        }

        public void Configure(MvcDataAnnotationsLocalizationOptions options)
        {
            options.DataAnnotationLocalizerProvider = (_, factory) => new MoxStringLocalizer(this._sources, factory);
        }
    }
}
