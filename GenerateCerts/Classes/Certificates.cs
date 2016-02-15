using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GenerateCerts.Classes;
using CERTENROLLLib;
using CERTCLILib;

namespace GenerateCerts.Classes
{
    public class Certificates : ObservableObject
    {

        //Some of these values : http://blogs.msdn.com/b/elee/archive/2009/03/21/enrolling-for-smartcard-certificates-across-domains.aspx
        //private const int CR_IN_BASE64 = 0x1;
        //private const int CR_IN_FORMATANY = 0;
        //private const int CR_DISP_ISSUED = 0x3;
        //private const int CR_DISP_UNDER_SUBMISSION = 0x5;
        
        private string _fqdn;
        private string _id;
        private string _status;
        private string _request;

        public string FQDN
        {
            get { return this._fqdn; }
            set
            {
                if (this._fqdn != value)
                {
                    this._fqdn = value;
                    NotifyPropertyChanged(() => this.FQDN);
                }
            }
        }

        public string ID
        {
            get { return this._id; }
            set
            {
                if (this._id != value)
                {
                    this._id = value;
                    NotifyPropertyChanged(() => this.ID);
                }
            }
        }

        public string Status
        {
            get { return this._status; }
            set
            {
                if (this._status != value)
                {
                    this._status = value;
                    NotifyPropertyChanged(() => this.Status);
                }
            }
        }

        public string Request
        {
            get { return this._request; }
            set
            {
                if (this._request != value)
                {
                    this._request = value;
                    NotifyPropertyChanged(() => this.Request);
                }
            }
        }


        
        public string CreateRequest(string cn, string ou, string o, string l, string s, string c, string oid, int keylength)
        {
            
                var objCSPs = new CCspInformations();
                objCSPs.AddAvailableCsps();

                var objPrivateKey = new CX509PrivateKey();
                objPrivateKey.Length = keylength;
                objPrivateKey.KeySpec = X509KeySpec.XCN_AT_SIGNATURE;                                                             //http://msdn.microsoft.com/en-us/library/windows/desktop/aa379409(v=vs.85).aspx
                objPrivateKey.KeyUsage = X509PrivateKeyUsageFlags.XCN_NCRYPT_ALLOW_ALL_USAGES;                                    //http://msdn.microsoft.com/en-us/library/windows/desktop/aa379417(v=vs.85).aspx
                objPrivateKey.MachineContext = false;                                                                             //http://msdn.microsoft.com/en-us/library/windows/desktop/aa379024(v=vs.85).aspx        
                objPrivateKey.ExportPolicy = X509PrivateKeyExportFlags.XCN_NCRYPT_ALLOW_EXPORT_FLAG;                              //http://msdn.microsoft.com/en-us/library/windows/desktop/aa379412(v=vs.85).aspx
                objPrivateKey.CspInformations = objCSPs;
                objPrivateKey.Create();

                var objPkcs10 = new CX509CertificateRequestPkcs10();
                objPkcs10.InitializeFromPrivateKey(
                    X509CertificateEnrollmentContext.ContextUser,                                                                 //http://msdn.microsoft.com/en-us/library/windows/desktop/aa379399(v=vs.85).aspx
                    objPrivateKey,
                    string.Empty);

                var objExtensionKeyUsage = new CX509ExtensionKeyUsage();
                objExtensionKeyUsage.InitializeEncode(
                    CERTENROLLLib.X509KeyUsageFlags.XCN_CERT_DIGITAL_SIGNATURE_KEY_USAGE |                                        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa379410(v=vs.85).aspx
                    CERTENROLLLib.X509KeyUsageFlags.XCN_CERT_NON_REPUDIATION_KEY_USAGE |                                            // http://msdn.microsoft.com/en-us/library/windows/desktop/aa379410(v=vs.85).aspx
                    CERTENROLLLib.X509KeyUsageFlags.XCN_CERT_KEY_ENCIPHERMENT_KEY_USAGE |                                        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa379410(v=vs.85).aspx
                    CERTENROLLLib.X509KeyUsageFlags.XCN_CERT_DATA_ENCIPHERMENT_KEY_USAGE);                                       // http://msdn.microsoft.com/en-us/library/windows/desktop/aa379410(v=vs.85).aspx   
            objPkcs10.X509Extensions.Add((CX509Extension)objExtensionKeyUsage);

                var objObjectId = new CObjectId();
                var objObjectIds = new CObjectIds();
                var objX509ExtensionEnhancedKeyUsage = new CX509ExtensionEnhancedKeyUsage();
                //objObjectId.InitializeFromValue("1.3.6.1.5.5.7.3.1");
                objObjectId.InitializeFromValue(oid);                                                                           //Some info about OIDS: http://www.alvestrand.no/objectid/1.3.6.1.5.5.7.3.html
                objObjectIds.Add(objObjectId);
                objX509ExtensionEnhancedKeyUsage.InitializeEncode(objObjectIds);
                objPkcs10.X509Extensions.Add((CX509Extension)objX509ExtensionEnhancedKeyUsage);


                // TODO: Create CERTS with SAN: http://msdn.microsoft.com/en-us/library/windows/desktop/aa378081(v=vs.85).aspx

            /*
                var test3 = new CX509ExtensionAlternativeNames();
                var test4 = new CAlternativeName();
            var test2 = new CAlternativeNames();

                test4.InitializeFromString(AlternativeNameType.XCN_CERT_ALT_NAME_DNS_NAME,"CRAP.no");
                test2.Add(test4);
                 test3.InitializeEncode(test2);
                */


                //objPkcs10.X509Extensions.Add((CX509Extension));

                var objDN = new CX500DistinguishedName();
                var subjectName = "CN = " + cn + ",OU = " + ou + ",O = " + o + ",L = " + l + ",S = " + s + ",C = " + c;

                objDN.Encode(subjectName, X500NameFlags.XCN_CERT_NAME_STR_NONE);                                                //http://msdn.microsoft.com/en-us/library/windows/desktop/aa379394(v=vs.85).aspx
                objPkcs10.Subject = objDN;

                var objEnroll = new CX509Enrollment();
                objEnroll.InitializeFromRequest(objPkcs10);
                var strRequest = objEnroll.CreateRequest(EncodingType.XCN_CRYPT_STRING_BASE64);                                 //http://msdn.microsoft.com/en-us/library/windows/desktop/aa374936(v=vs.85).aspx

                return strRequest;
            
        }


        public string SendRequest(string request, string caserver)
        {

            var objCertRequest = new CCertRequest();
            var iDisposition = objCertRequest.Submit(
                    (int)Encoding.CR_IN_BASE64 | (int)Format.CR_IN_FORMATANY,                                                                            //http://msdn.microsoft.com/en-us/library/windows/desktop/aa385054(v=vs.85).aspx
                    request,
                    string.Empty,
                    caserver);

            return objCertRequest.GetRequestId().ToString();

        }


        public string RetrieveCertStatus(int id, string caserver)
        {
            
                int strDisposition;
                string msg = "";

                CCertRequest objCertRequest = new CCertRequestClass();
                strDisposition = objCertRequest.RetrievePending(id, caserver);

                switch (strDisposition)
                {
                    case (int)RequestDisposition.CR_DISP_INCOMPLETE:
                        msg = "incomplete certificate";
                        break;
                    case (int)RequestDisposition.CR_DISP_DENIED:
                        msg = "request denied";
                        break;
                    case (int)RequestDisposition.CR_DISP_ISSUED:
                        msg = "certificate issued";
                        break;
                    case (int)RequestDisposition.CR_DISP_UNDER_SUBMISSION:
                        msg = "request pending";
                        break;
                    case (int)RequestDisposition.CR_DISP_REVOKED:
                        msg = "certificate revoked";
                        break;
                }

                
                return msg;
            
        }


        public string CreateTemplateRequest(string cn, string ou, string o, string l, string s, string c, int keylength, string template)
        {
           
                var objCSPs = new CCspInformations();
                objCSPs.AddAvailableCsps();
                var objPrivateKey = new CX509PrivateKey();
                objPrivateKey.Length = keylength;
                objPrivateKey.KeySpec = X509KeySpec.XCN_AT_SIGNATURE;
                objPrivateKey.KeyUsage = X509PrivateKeyUsageFlags.XCN_NCRYPT_ALLOW_ALL_USAGES;
                objPrivateKey.MachineContext = false;
                objPrivateKey.ExportPolicy = X509PrivateKeyExportFlags.XCN_NCRYPT_ALLOW_EXPORT_FLAG;
                objPrivateKey.CspInformations = objCSPs;
                objPrivateKey.Create();

                var objPkcs10 = new CX509CertificateRequestPkcs10();
                objPkcs10.InitializeFromPrivateKey(
                    X509CertificateEnrollmentContext.ContextUser,
                    objPrivateKey,
                    template);

                var objDN = new CX500DistinguishedName();
                
                var subjectName = "CN = " + cn + ",OU = " + ou + ",O = " + o + ",L = " + l + ",S = " + s + ",C = " + c;
                objDN.Encode(subjectName, X500NameFlags.XCN_CERT_NAME_STR_NONE);
                objPkcs10.Subject = objDN;

                var objEnroll = new CX509Enrollment();
                objEnroll.InitializeFromRequest(objPkcs10);
                var strRequest = objEnroll.CreateRequest(EncodingType.XCN_CRYPT_STRING_BASE64);

                return strRequest;
           
        }



        //Some of these values : http://blogs.msdn.com/b/elee/archive/2009/03/21/enrolling-for-smartcard-certificates-across-domains.aspx
        public enum RequestDisposition
        {
            CR_DISP_INCOMPLETE = 0,
            CR_DISP_ERROR = 0x1,
            CR_DISP_DENIED = 0x2,
            CR_DISP_ISSUED = 0x3,
            CR_DISP_ISSUED_OUT_OF_BAND = 0x4,
            CR_DISP_UNDER_SUBMISSION = 0x5,
            CR_DISP_REVOKED = 0x6,
            CCP_DISP_INVALID_SERIALNBR = 0x7,
            CCP_DISP_CONFIG = 0x8,
            CCP_DISP_DB_FAILED = 0x9
        }
        public enum Encoding
        {
            CR_IN_BASE64HEADER = 0x0,
            CR_IN_BASE64 = 0x1,
            CR_IN_BINARY = 0x2,
            CR_IN_ENCODEANY = 0xff,
            CR_OUT_BASE64HEADER = 0x0,
            CR_OUT_BASE64 = 0x1,
            CR_OUT_BINARY = 0x2
        }
        public enum Format
        {
            CR_IN_FORMATANY = 0x0,
            CR_IN_PKCS10 = 0x100,
            CR_IN_KEYGEN = 0x200,
            CR_IN_PKCS7 = 0x300,
            CR_IN_CMC = 0x400
        }
        public enum CertificateConfiguration
        {
            CC_DEFAULTCONFIG = 0x0,
            CC_UIPICKCONFIG = 0x1,
            CC_FIRSTCONFIG = 0x2,
            CC_LOCALCONFIG = 0x3,
            CC_LOCALACTIVECONFIG = 0x4,
            CC_UIPICKCONFIGSKIPLOCALCA = 0x5
        }


        public class OID
        {
            public string oid { get; set; }
            public string name { get; set; }
        }

        public List<OID> ListOids()
        {
            List<OID> items = new List<OID>();

            items.Add(new OID { name = "Server Authentication", oid = "1.3.6.1.5.5.7.3.1" });
            items.Add(new OID { name = "Client Authentication", oid = "1.3.6.1.5.5.7.3.2" });
            items.Add(new OID { name = "CodeSigning", oid = "1.3.6.1.5.5.7.3.3" });
            items.Add(new OID { name = "Email Protection", oid = "1.3.6.1.5.5.7.3.4" });
            items.Add(new OID { name = "IPSEC EndSystem", oid = "1.3.6.1.5.5.7.3.5" });
            items.Add(new OID { name = "IPSEC Tunnel", oid = "1.3.6.1.5.5.7.3.6" });
            items.Add(new OID { name = "IPSEC User", oid = "1.3.6.1.5.5.7.3.7" });
            items.Add(new OID { name = "TimeStamping", oid = "1.3.6.1.5.5.7.3.8" });
            items.Add(new OID { name = "OCSPSigning", oid = "1.3.6.1.5.5.7.3.9" });

            return items;
        }

        /*  1.3.6.1.5.5.7.3.1 - id_kp_serverAuth
                     *  1.3.6.1.5.5.7.3.2 - id_kp_clientAuth
                     *  1.3.6.1.5.5.7.3.3 - id_kp_codeSigning
                     *  1.3.6.1.5.5.7.3.4 - id_kp_emailProtection
                     *  1.3.6.1.5.5.7.3.5 - id-kp-ipsecEndSystem
                     *  1.3.6.1.5.5.7.3.6 - id-kp-ipsecTunnel
                     *  1.3.6.1.5.5.7.3.7 - id-kp-ipsecUser
                     *  1.3.6.1.5.5.7.3.8 - id_kp_timeStamping
                     *  1.3.6.1.5.5.7.3.9 - OCSPSigning
        */

    }
}
