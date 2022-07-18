using System.Collections.Generic;

namespace Barragem.Models
{
    public class ImprimirGruposModel
    {
        public ImprimirGruposModel()
        {
            Categorias = new List<ItemCategoriaModel>();
        }

        public string NomeTorneio { get; set; }
        public List<ItemCategoriaModel> Categorias { get; set; }

        public class ItemCategoriaModel
        {
            public ItemCategoriaModel()
            {
                Grupos = new List<ItemGruposModel>();
            }
            public string NomeCategoria { get; set; }
            public List<ItemGruposModel> Grupos { get; set; }
        }

        public class ItemGruposModel 
        {
            public ItemGruposModel()
            {
                Inscritos = new List<string>();
            }

            public string NomeGrupo { get; set; }

            public List<string> Inscritos { get; set; }
        }
    }
}