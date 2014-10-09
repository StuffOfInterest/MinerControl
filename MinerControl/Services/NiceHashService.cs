namespace MinerControl.Services
{
    public class NiceHashService : NiceHashServiceBase
    {
        public NiceHashService()
        {
            ServiceEnum = ServiceEnum.NiceHash;
        }

        protected override string BalanceFormat { get { return "https://www.nicehash.com/api?method=stats.provider&addr={0}"; } }
        protected override string CurrentFormat { get { return "https://www.nicehash.com/api?method=stats.global.current"; } }
    }
}
