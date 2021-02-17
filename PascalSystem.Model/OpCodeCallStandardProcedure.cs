// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo
namespace PascalSystem.Model
{
    using System;

    public partial class OpCode
    {
        public class CallStandardProcedure : OpCode
        {
            public enum StandardCall : byte
            {
                /// <summary>
                ///     IoCheck()
                /// </summary>
                CSP_IOC = 0,

                /// <summary>
                ///     new()
                /// </summary>
                CSP_NEW = 1,

                /// <summary>
                ///     moveleft()
                /// </summary>
                CSP_MVL = 2,

                /// <summary>
                ///     moveright()
                /// </summary>
                CSP_MVR = 3,

                /// <summary>
                ///     exit()
                /// </summary>
                CSP_XIT = 4,

                /// <summary>
                ///     unitread()
                /// </summary>
                CSP_UREAD = 5,

                /// <summary>
                ///     unitwrite()
                /// </summary>
                CSP_UWRITE = 6,

                /// <summary>
                ///     idsearch()
                /// </summary>
                CSP_IDS = 7,

                /// <summary>
                ///     treesearch()
                /// </summary>
                CSP_TRS = 8,

                /// <summary>
                ///     time()
                /// </summary>
                CSP_TIM = 9,

                /// <summary>
                ///     fillchar()
                /// </summary>
                CSP_FLC = 10,

                /// <summary>
                ///     scan()
                /// </summary>
                CSP_SCN = 11,

                /// <summary>
                ///     unitstat()
                /// </summary>
                CSP_USTAT = 12,

                /// <summary>
                ///     LoadSegment()
                /// </summary>
                CSP_LDSEG = 21,

                /// <summary>
                ///     UnloadSegment()
                /// </summary>
                CSP_ULDSEG = 22,

                /// <summary>
                ///     trunc()
                /// </summary>
                CSP_TRC = 23,

                /// <summary>
                ///     round()
                /// </summary>
                CSP_RND = 24,

                /// <summary>
                ///     sin()
                /// </summary>
                CSP_SIN = 25,

                /// <summary>
                ///     cos()
                /// </summary>
                CSP_COS = 26,

                /// <summary>
                ///     tan()
                /// </summary>
                CSP_TAN = 27,

                /// <summary>
                ///     atan()
                /// </summary>
                CSP_ATAN = 28,

                /// <summary>
                ///     ln()
                /// </summary>
                CSP_LN = 29,

                /// <summary>
                ///     exp()
                /// </summary>
                CSP_EXP = 30,

                /// <summary>
                ///     sqrt()
                /// </summary>
                CSP_SQRT = 31,

                /// <summary>
                ///     mark()
                /// </summary>
                CSP_MRK = 32,

                /// <summary>
                ///     release()
                /// </summary>
                CSP_RLS = 33,

                /// <summary>
                ///     ioresult()
                /// </summary>
                CSP_IOR = 34,

                /// <summary>
                ///     unitbusy()
                /// </summary>
                CSP_UBUSY = 35,

                /// <summary>
                ///     PwrOfTen()
                /// </summary>
                CSP_POT = 36,

                /// <summary>
                ///     unitwait()
                /// </summary>
                CSP_UWAIT = 37,

                /// <summary>
                ///     unitclear()
                /// </summary>
                CSP_UCLEAR = 38,

                /// <summary>
                ///     halt()
                /// </summary>
                CSP_HLT = 39,

                /// <summary>
                ///     memavail()
                /// </summary>
                CSP_MAV = 40
            }

            public CallStandardProcedure(int subCode) : base(OpcodeValue.CSP) => this.SubType =
                (StandardCall)subCode;

            public StandardCall SubType { get; }

            public override int Length => 2;

            public override int GetHashCode() => base.GetHashCode() ^ (int)this.SubType << 8;

            public override string ToString() => base.ToString() +
                                                 Enum.GetName(typeof(StandardCall), this.SubType)?.Substring(4);
        }
    }
}