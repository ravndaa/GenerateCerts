using CERTCLILib;
using System;
using System.Windows;
using GenerateCerts.Classes;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using CERTENROLLLib;
using GenerateCerts.Windows;

namespace GenerateCerts
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        //Static Values
        private const int CC_UIPICKCONFIG = 0x1;
        private const int CR_OUT_BASE64 = 0x1;
        private const int CR_OUT_CHAIN = 0x100;

        //
        Templates templates = new Templates();
        Certificates Certificat = new Certificates();
        public ObservableCollection<Certificates> Certs = new ObservableCollection<Certificates>();

        public MainWindow()
        {
            InitializeComponent();

            Certs = new ObservableCollection<Certificates>();
            lst_certs.ItemsSource = Certs;

        }

        private void btn_SelectCA_Click(object sender, RoutedEventArgs e)
        {

            CCertConfig objCertConfig = new CCertConfigClass();
            CCertRequest objCertRequest = new CCertRequestClass();
            
            try
            {
                // Get CA config from UI
                string strCAConfig = objCertConfig.GetConfig(CC_UIPICKCONFIG);
                
                if(String.IsNullOrWhiteSpace(strCAConfig))
                {
                    return;
                }
                
            

            
            // Get CA Connection string
            string CACon = objCertConfig.GetField("Config");
            txt_CAServer.Text = CACon;

            // Get CA Type
            string caType = objCertRequest.GetCAProperty(strCAConfig, 10, 0, 1, 0).ToString();
            string caTypeTXT = "";
            switch (caType)
            {
                case "0":
                    caTypeTXT = "ENTERPRISE ROOT CA";
                    break;
                case "1":
                    caTypeTXT = "ENTERPRISE SUB CA";
                    break;
                case "3":
                    caTypeTXT = "STANDALONE ROOT CA";
                    break;
                case "4":
                    caTypeTXT = "STANDALONE SUB CA";
                    break;
            }
            txt_CaType.Text = caTypeTXT;

            if (caType == "3" || caType == "4" || caType == "5")
            {
                cmb_Templates.Visibility = System.Windows.Visibility.Hidden;
                btn_LoadTempls.Visibility = System.Windows.Visibility.Hidden;
                oids.Visibility = System.Windows.Visibility.Visible;
                txt_oid.Visibility = System.Windows.Visibility.Visible;
                oids.ItemsSource = Certificat.ListOids();

                strength.Visibility = System.Windows.Visibility.Visible;
            }
            else if (caType == "0" || caType == "1")
            {
                cmb_Templates.Visibility = System.Windows.Visibility.Visible;
                oids.Visibility = System.Windows.Visibility.Hidden;
                txt_oid.Visibility = System.Windows.Visibility.Hidden;
                btn_LoadTempls.Visibility = System.Windows.Visibility.Visible;
                cmb_Templates.ItemsSource = templates.GetCaTemplates(strCAConfig);
                strength.Visibility = System.Windows.Visibility.Visible;
            }

            }
            catch(Exception ex)
            {
                
                //Check if the user closed the dialog. Do nothing.
                if (ex.HResult.ToString() == "-2147023673")
                {
                    //MessageBox.Show("Closed By user");
                }
                    //Check if there is no available CA Servers.
                else if (ex.HResult.ToString() == "-2147024637")
                {
                    MessageBox.Show("Can't find available Servers");
                }
                    // If unknown error occurs.
                else
                {
                    MessageBox.Show(ex.Message + " " + ex.HResult.ToString());
                }
            }

        }

        private void btn_LoadTempls_Click(object sender, RoutedEventArgs e)
        {

            string caserver = txt_CAServer.Text.ToString();
            cmb_Templates.ItemsSource = templates.GetCaTemplates(caserver);
        }

        private void btn_generate_Click(object sender, RoutedEventArgs e)
        {

            //Check if server exist
            if (string.IsNullOrEmpty(txt_CAServer.Text))
            {
                MessageBox.Show("No server specified");
                return;
            }
            //Check if CA Type Exist
            if (string.IsNullOrEmpty(txt_CaType.Text.ToString()))
            {
                MessageBox.Show("Sorry, not sure what type of server this is.");
                return;
            }
            else
            {
                //Check if Template exist and Type contains ENTERPRISE
                if (string.IsNullOrEmpty(cmb_Templates.Text) && txt_CaType.Text.ToString().Contains("ENTERPRISE"))
                {
                    MessageBox.Show("Please Select Template");
                    return;
                }
            }

            string caserver = txt_CAServer.Text;

            if(Certs.Count == 0)
            {
                MessageBox.Show("No Request(s) To generate");
                return;
            }

            foreach (Certificates c in Certs)
            {

                if (txt_CaType.Text.ToString().Contains("ENTERPRISE"))
                {
                    string templ = cmb_Templates.SelectedValue.ToString();
                    int strenght = Convert.ToInt32(strength.SelectedValue.ToString());

                    c.Request = Certificat.CreateTemplateRequest(c.FQDN, "", "", "", "", "", strenght, templ);
                }
                else if (txt_CaType.Text.ToString().Contains("STANDALONE"))
                {
                    // Where to find some OIDS : https://access.redhat.com/documentation/en-US/Red_Hat_Certificate_System/8.0/html/Admin_Guide/Standard_X.509_v3_Certificate_Extensions.html

                    /*
                     *   Server authentication	1.3.6.1.5.5.7.3.1
                     *   Client authentication	1.3.6.1.5.5.7.3.2
                     *   Code signing	1.3.6.1.5.5.7.3.3
                     *   Email	1.3.6.1.5.5.7.3.4
                     *   Timestamping	1.3.6.1.5.5.7.3.8
                     * 
                     *  http://www.alvestrand.no/objectid/1.3.6.1.5.5.7.3.html
                     *  1.3.6.1.5.5.7.3.1 - id_kp_serverAuth
                     *  1.3.6.1.5.5.7.3.2 - id_kp_clientAuth
                     *  1.3.6.1.5.5.7.3.3 - id_kp_codeSigning
                     *  1.3.6.1.5.5.7.3.4 - id_kp_emailProtection
                     *  1.3.6.1.5.5.7.3.5 - id-kp-ipsecEndSystem
                     *  1.3.6.1.5.5.7.3.6 - id-kp-ipsecTunnel
                     *  1.3.6.1.5.5.7.3.7 - id-kp-ipsecUser
                     *  1.3.6.1.5.5.7.3.8 - id_kp_timeStamping
                     *  1.3.6.1.5.5.7.3.9 - OCSPSigning
                     *    
                     * 
                     * 
                     */

                    int strenght = Convert.ToInt32(strength.SelectedValue.ToString());
                    c.Request = Certificat.CreateRequest(c.FQDN, "", "", "", "", "", txt_oid.Text, strenght);
                }
                else
                {
                    MessageBox.Show("Something Went VERY Wrong");
                    return;
                }

                if (c.Request != null)
                {
                    c.ID = Certificat.SendRequest(c.Request, caserver);

                    if (c.ID != null)
                    {
                        int cid = Convert.ToInt32(c.ID);
                        c.Status = Certificat.RetrieveCertStatus(cid, caserver);
                    }

                }

            }

        }

        private void btn_savepfx_Click(object sender, RoutedEventArgs e)
        {



            string passwd = txt_Pfxpasswd.Password;
            string caserver = txt_CAServer.Text;
            string dir = Directory.GetParent(Assembly.GetExecutingAssembly().Location).ToString();

            if (Certs.Count == 0)
            {
                MessageBox.Show("No Request(s) To Save");
                return;
            }

            foreach (Certificates c in Certs)
            {
                if (c.Status != "File Created!" && c.Status == "certificate issued")
                {
                    
                
                CX509Enrollment objEnroll = new CX509EnrollmentClass();
                var objCertRequest = new CCertRequest();

                var iDisposition = objCertRequest.RetrievePending(Convert.ToInt32(c.ID), caserver);

                if (Convert.ToInt32(iDisposition) == 3)
                {
                    var cert = objCertRequest.GetCertificate(CR_OUT_BASE64 | CR_OUT_CHAIN);

                    objEnroll.Initialize(X509CertificateEnrollmentContext.ContextUser);
                    objEnroll.InstallResponse(
                        InstallResponseRestrictionFlags.AllowUntrustedRoot,
                        cert,
                        EncodingType.XCN_CRYPT_STRING_BASE64,
                        null
                    );

                    c.Status = "File Created!";

                    var fil = objEnroll.CreatePFX(passwd, PFXExportOptions.PFXExportChainWithRoot, EncodingType.XCN_CRYPT_STRING_BASE64);
                    System.IO.File.WriteAllText(dir + @"\" + c.FQDN + ".pfx", fil);
                }

            }

            }


        }

        private void btn_savecer_Click(object sender, RoutedEventArgs e)
        {


            string caserver = txt_CAServer.Text;
            string dir = Directory.GetParent(Assembly.GetExecutingAssembly().Location).ToString();

            if (Certs.Count == 0)
            {
                MessageBox.Show("No Request(s) To Save");
                return;
            }

            foreach (Certificates c in Certs)
            {
                var objCertRequest = new CCertRequest();
                int reqid = Convert.ToInt32(c.ID);

                var iDisposition = objCertRequest.RetrievePending(reqid, caserver);
                if (Convert.ToInt32(iDisposition) == 3)
                {
                    string cert = objCertRequest.GetCertificate(0);
                    System.IO.File.WriteAllText(dir + @"\" + c.FQDN + ".cer", cert);

                    c.Status = "File Created!";
                }
            }

        }

        private void btn_retrStatus_Click(object sender, RoutedEventArgs e)
        {

            string caserver = txt_CAServer.Text;

            if (Certs.Count == 0)
            {
                MessageBox.Show("No Request(s) To Save");
                return;
            }

            foreach (Certificates c in Certs)
            {

                if (c.ID != null)
                {
                    int cid = Convert.ToInt32(c.ID);
                    c.Status = Certificat.RetrieveCertStatus(cid, caserver);
                }

            }

        }

        private void btn_loadtxt_Click(object sender, RoutedEventArgs e)
        {
            LoadTxt w = new LoadTxt();
            

            if (w.ShowDialog() == false)
            {
                if (w.clicked == "cancel")
                {
                    return;
                }

                string i = w.lines;

                if(w.resetlist == true)
                {
                    Certs.Clear();
                }

                String[] lines = i.Split('\n');
                foreach (string l in lines)
                {
                    if (!string.IsNullOrWhiteSpace(l))
                    {
                        //MessageBox.Show(l);
                        Certs.Add(new Certificates { FQDN = l.Replace("\r","") });
                    }

                }


            }

        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            About a = new About();
            a.ShowDialog();
        }

        
    }
}
