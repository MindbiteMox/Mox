using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mindbite.Mox.Verification.Services
{
    public class VerificationError
    {
        public string Code { get; set; }
        public string Description { get; set; }
    }

    public class VerificationResult
    {
        public bool Success { get; set; }
        public IEnumerable<VerificationError> Errors { get; set; }
        public IVerificator Verificator { get; set; }
    }

    public interface IVerificator
    {
        Task<VerificationResult> VerifyAsync(IServiceProvider serviceProvider);
        string Name { get; }
        int Order => 0;
    }

    public class VerificationOptions
    {
        public List<IVerificator> Verificators { get; private set; }

        public VerificationOptions()
        {
            this.Verificators = new List<IVerificator>();
        }
    }
}
