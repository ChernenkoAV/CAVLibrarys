using System.Reflection;
using System.ServiceModel.Security;
using Cav.ReflectHelpers;

namespace Cav.DigitalSignature
{
    /// <summary>
    /// Реализация ЭЦП по нашим гостам по ЭЦП средствами КриптоПро
    /// </summary>
    public sealed class CryptoProRutine
    {
        private static object lockObj = new object();
        private static Assembly cryptoProSharpeiServiceModel;
        /// <summary>
        /// Получение GostAlgorithmSuite.BasicGostObsolete из КриптоПрошной сборки с отложенной загрузкой.
        /// </summary>
        /// <remarks>
        /// CryptoPro.Sharpei.ServiceModel должен быть в GAC.
        /// Используется CryptoPro.Sharpei.ServiceModel, Version=1.4.0.1, Culture=neutral, PublicKeyToken=473b8c5086e795f5, processorArchitecture=MSIL
        /// </remarks>
        public static SecurityAlgorithmSuite CriptoProBasicGostObsolete
        {
            get
            {
                if (criptoProBasicGostObsolete == null)
                    lock (lockObj)
                    {
                        if (cryptoProSharpeiServiceModel == null)
                            cryptoProSharpeiServiceModel = Assembly.Load("CryptoPro.Sharpei.ServiceModel, Version=1.4.0.1, Culture=neutral, PublicKeyToken=473b8c5086e795f5, processorArchitecture=MSIL");
                        criptoProBasicGostObsolete = (SecurityAlgorithmSuite)cryptoProSharpeiServiceModel.GetStaticOrConstPropertyOrFieldValue("GostAlgorithmSuite", "BasicGostObsolete");
                    }

                return criptoProBasicGostObsolete;
            }
        }

        private static SecurityAlgorithmSuite criptoProBasicGostObsolete;

        /// <summary>
        /// Получение Gost2012_256AlgorithmSuite.BasicGost из КриптоПрошной сборки с отложенной загрузкой.
        /// </summary>
        /// <remarks>
        /// CryptoPro.Sharpei.ServiceModel должен быть в GAC.
        /// Используется CryptoPro.Sharpei.ServiceModel, Version=1.4.0.1, Culture=neutral, PublicKeyToken=473b8c5086e795f5, processorArchitecture=MSIL
        /// </remarks>
        public static SecurityAlgorithmSuite CriptoProGost2012_256BasicGost
        {
            get
            {
                if (criptoProGost2012_256BasicGost == null)
                    lock (lockObj)
                    {
                        if (cryptoProSharpeiServiceModel == null)
                            cryptoProSharpeiServiceModel = Assembly.Load("CryptoPro.Sharpei.ServiceModel, Version=1.4.0.1, Culture=neutral, PublicKeyToken=473b8c5086e795f5, processorArchitecture=MSIL");

                        criptoProGost2012_256BasicGost = (SecurityAlgorithmSuite)cryptoProSharpeiServiceModel.GetStaticOrConstPropertyOrFieldValue("Gost2012_256AlgorithmSuite", "BasicGost");
                    }

                return criptoProGost2012_256BasicGost;
            }
        }

        private static SecurityAlgorithmSuite criptoProGost2012_256BasicGost;

        /// <summary>
        /// Получение Gost2012_512AlgorithmSuite.BasicGost из КриптоПрошной сборки с отложенной загрузкой.
        /// </summary>
        /// <remarks>
        /// CryptoPro.Sharpei.ServiceModel должен быть в GAC.
        /// Используется CryptoPro.Sharpei.ServiceModel, Version=1.4.0.1, Culture=neutral, PublicKeyToken=473b8c5086e795f5, processorArchitecture=MSIL
        /// </remarks>
        public static SecurityAlgorithmSuite CriptoProGost2012_512BasicGost
        {
            get
            {
                if (criptoProGost2012_512BasicGost == null)
                    lock (lockObj)
                    {
                        if (cryptoProSharpeiServiceModel == null)
                            cryptoProSharpeiServiceModel = Assembly.Load("CryptoPro.Sharpei.ServiceModel, Version=1.4.0.1, Culture=neutral, PublicKeyToken=473b8c5086e795f5, processorArchitecture=MSIL");

                        criptoProGost2012_512BasicGost = (SecurityAlgorithmSuite)cryptoProSharpeiServiceModel.GetStaticOrConstPropertyOrFieldValue("Gost2012_512AlgorithmSuite", "BasicGost");
                    }

                return criptoProGost2012_512BasicGost;
            }
        }

        private static SecurityAlgorithmSuite criptoProGost2012_512BasicGost;
    }
}
