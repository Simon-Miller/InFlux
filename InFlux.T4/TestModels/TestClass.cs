using InFlux.Attributes;
using System.ComponentModel.DataAnnotations;

namespace InFlux.T4.TestModels
{
    [AutoWireup]
    public partial class TestClass
    {
        [Required]
        private int id;

        private string name = string.Empty;
    }
}
