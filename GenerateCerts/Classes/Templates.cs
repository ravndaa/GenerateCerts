using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GenerateCerts.Classes;
using System.Text.RegularExpressions;
using CERTCLILib;
using System.Collections.ObjectModel;


namespace GenerateCerts.Classes
{
    public class Templates : ObservableObject
    {

        private string _template;

        public string Template
        {
            get { return this._template; }
            set
            {
                if (this._template != value)
                {
                    this._template = value;
                    NotifyPropertyChanged(() => this.Template);
                }
            }
        }


        public ObservableCollection<Templates> GetCaTemplates(string caserver)
        {
           
                CCertRequest objCertRequest = new CCertRequestClass();
                ObservableCollection<Templates> Templates = new ObservableCollection<Templates>();

                Regex regex = new Regex(@"([A-Za-z]+)");
                string value = objCertRequest.GetCAProperty(caserver, 29, 0, 4, 0).ToString();
                string[] lines = Regex.Split(value, @"\n");

                foreach (string line in lines)
                {
                    Match match = regex.Match(line);
                    if (match.Success)
                    {
                        Templates.Add(new Templates { Template = line });
                    }
                }

                return Templates;
            
        }


    }
}
