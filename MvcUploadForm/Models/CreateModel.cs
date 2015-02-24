using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace MvcUploadForm.Models
{
    public class CreateModel
    {

        [Required]
        public List<HttpPostedFileBase> File { get; set; }

        public List<string> Password { get; set; }


    }
}