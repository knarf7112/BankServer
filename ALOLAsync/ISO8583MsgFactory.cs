using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//
using IM.ISO8583.Utility;
using Kms.Crypto;
using OL_Autoload_Lib;

namespace ALOLAsync
{
    public class ISO8583MsgFactory
    {
        #region Field 宣告轉換ISO8583用的演算法物件

        Iso8583InfoGetter iso8583InfoGetter;
        Iso8583InfoGetter df61InfoGetter;

        BitWorker bitWorker;
        BitWorker df61BitWorker;

        BitMapHelper bitMapHelper;

        MainMsgWorker mainMsgWorker;
        Df61MsgWorker df61MsgWorker;
        #endregion

        public ISO8583MsgFactory()
        {
            //初始化演算法物件
            iso8583InfoGetter = new Iso8583InfoGetter("IM.ISO8583.Utility.Config.iso8583Fn.xml",
                                                     @"//Message[@name='Common' and @peer='Common']");
            df61InfoGetter = new Iso8583InfoGetter("IM.ISO8583.Utility.Config.iso8583Fn.xml",
                                                  @"//Message[@name='DF61' and @peer='Common']");
            
            bitWorker = new BitWorker(iso8583InfoGetter);
            df61BitWorker = new BitWorker(df61InfoGetter);

            bitMapHelper = new BitMapHelper()
            {
                BitMapper = new BitMapper() { HexConverter = new HexConverter() },
                HexConverter = new HexConverter()
            };

            mainMsgWorker = new MainMsgWorker()
            {
                BitMapHelper = bitMapHelper,
                BitWorker = bitWorker
            };

            df61MsgWorker = new Df61MsgWorker()
            {
                BitMapHelper = bitMapHelper,
                Df61BitWorker = df61BitWorker
            };
        }

        /// <summary>
        /// 要求物件轉換要求電文(授權/代行授權/沖正授權)
        /// </summary>
        /// <param name="messageType">要求格式(0100/0120/0121|0420/0421)</param>
        /// <param name="requestToBank">要求(授權/代行授權/沖正授權)物件</param>
        /// <returns>Response to Bank電文</returns>
        public string GetRequestMsg(string messageType, AutoloadRqt_2Bank requestToBank)
        {
            try
            {
                switch (messageType)
                {
                    case "0100":
                    case "0120":
                    case "0121":
                        return ConvertALOL(requestToBank);
                    case "0420":
                    case "0421":
                        return ConvertRALOL(requestToBank);
                    default:
                        throw new Exception("[GetRequestMsg] Message Type not defined:" + messageType);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //TODO..................
        public string GetResponseMsg(string messagetype, AutoloadRqt_FBank requestToBank)
        {
            try
            {
                return null;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 沖正Request
        /// </summary>
        /// <param name="requestToBank">沖正物件</param>
        /// <returns>沖正電文</returns>
        private string ConvertRALOL(AutoloadRqt_2Bank requestToBank)
        {
            try
            {
                Console.WriteLine("開始轉換沖正物件");
                //Message Header
                string fromTo = "8888" + requestToBank.BANK_CODE.PadLeft(4, '0');//8888表示愛金卡機構代號
                //initial BitMap List
                string[] srcList = new string[129];
                for (int i = 0; i < srcList.Length; i++)
                {
                    srcList[i] = "";
                }
                srcList[2] = requestToBank.ICC_NO;//"0417149984000007"//"0000000000000000";
                srcList[3] = requestToBank.PROCESSING_CODE;//"990174"//"990174";
                srcList[4] = requestToBank.AMOUNT;//"000000000500"//"000000000055";
                srcList[7] = requestToBank.TRANS_DATETIME;//"0115135959"//"0128180006";
                srcList[11] = requestToBank.STAN;//"005009"//"666666";
                srcList[32] = requestToBank.STORE_NO;//"st00896159"// "st00000001";
                srcList[37] = requestToBank.RRN;//"501513005009"//"502818666666";
                srcList[41] = requestToBank.POS_NO;//"00000001"//"00000001";
                srcList[42] = requestToBank.MERCHANT_NO;//"000000022555003"//"000000022555003";

                //init Field 61
                string[] srcListDf61 = new string[65];
                for (int i = 0; i < srcListDf61.Length; i++)
                {
                    srcListDf61[i] = "";
                }
                srcListDf61[9] = requestToBank.ICC_info.RETURN_CODE;//"00000000"//"00000000";
                MsgContext msgContextDf61 = df61MsgWorker.Build(null, null, srcListDf61);

                srcList[61] = msgContextDf61.SrcMessage;//"808000000000000000000000" //"008000000000000000000000";
                srcList[90] = requestToBank.ORI_dtat.MESSAGE_TYPE + requestToBank.ORI_dtat.TRANSACTION_DATE + requestToBank.ORI_dtat.STAN + requestToBank.ORI_dtat.STORE_NO + requestToBank.ORI_dtat.RRN + "  ";
                //"0120" + "0115135959" + "005002" + "00896159" + "501513005002" + "  ";
                //"0100" + "0128183005" + "555555" + "00000001" + "502818666666" + "  ";

                MsgContext msgResult = mainMsgWorker.Build(fromTo, requestToBank.MESSAGE_TYPE, srcList);//"88880000","0420"
                Console.WriteLine("轉換後的銀行訊息(Length:" + msgResult.SrcMessage.Length + "): " + msgResult.SrcMessage);
                return msgResult.SrcMessage;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 請求授權(自動加值/代行)
        /// </summary>
        /// <param name="requestToBank">請求授權物件</param>
        /// <returns>請求授權電文</returns>
        private string ConvertALOL(AutoloadRqt_2Bank requestToBank)
        {
            try
            {
                Console.WriteLine("開始轉換要求授權物件");
                //Message Header
                string fromTo = "8888" + requestToBank.BANK_CODE.PadLeft(4, '0');//8888表示愛金卡機構代號
                //initial BitMap List
                string[] srcList = new string[129];
                for (int i = 0; i < srcList.Length; i++)
                {
                    srcList[i] = "";
                }
                srcList[2] = requestToBank.ICC_NO;//"0417149984000007"//"0000000000000000";
                srcList[3] = requestToBank.PROCESSING_CODE;//"990174"//"990174";
                srcList[4] = requestToBank.AMOUNT;//"000000000500"//"000000000055";
                srcList[7] = requestToBank.TRANS_DATETIME;//"0115135959"//"0128180006";
                srcList[11] = requestToBank.STAN;//"005009"//"666666";
                srcList[32] = requestToBank.STORE_NO;//"st00896159"// "st00000001";
                srcList[37] = requestToBank.RRN;//"501513005009"//"502818666666";
                srcList[41] = requestToBank.POS_NO;//"00000001"//"00000001";
                srcList[42] = requestToBank.MERCHANT_NO;//"000000022555003"//"000000022555003";

                //init Field 61
                string[] srcListDf61 = new string[65];
                for (int i = 0; i < srcListDf61.Length; i++)
                {
                    srcListDf61[i] = "";
                }
                srcListDf61[3] = requestToBank.ICC_info.STORE_NO;   // 8碼
                srcListDf61[4] = requestToBank.ICC_info.REG_ID;     // 3碼
                srcListDf61[8] = requestToBank.ICC_info.TX_DATETIME;//14碼
                srcListDf61[10] = requestToBank.ICC_info.ICC_NO;    //16碼
                srcListDf61[11] = requestToBank.ICC_info.AMT;       // 8碼,"00000000"//"00000000";
                srcListDf61[35] = requestToBank.ICC_info.NECM_ID;   //20碼
                MsgContext msgContextDf61 = df61MsgWorker.Build(null, null, srcListDf61);

                srcList[61] = msgContextDf61.SrcMessage;            //16碼"808000000000000000000000" //"008000000000000000000000";
                //"0120" + "0115135959" + "005002" + "00896159" + "501513005002" + "  ";
                //"0100" + "0128183005" + "555555" + "00000001" + "502818666666" + "  ";

                MsgContext msgResult = mainMsgWorker.Build(fromTo, requestToBank.MESSAGE_TYPE, srcList);//"88880000","0420"
                Console.WriteLine("轉換後的銀行訊息(Length:" + msgResult.SrcMessage.Length + "): " + msgResult.SrcMessage);
                return msgResult.SrcMessage;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
