using LuceneServerNET.Core.Models.Mapping;
using LuceneServerNET.Core.Phonetics;

namespace gView.Cmd.FillLuceneServer
{
    class SearchIndexMapping : IndexMapping
    {
        public SearchIndexMapping(Algorithm algorithm, char[] encodeCharacters)
        {
            this.AddField(new IndexField("id", FieldTypes.StringType));
            this.AddField(new IndexField("suggested_text", FieldTypes.TextType));
            this.AddField(new IndexField("subtext", FieldTypes.TextType));
            //this.AddField(new IndexField("category", FieldTypes.StringType));
            this.AddField(new IndexField("category", FieldTypes.TextType));

            //this.AddField(new IndexField("longitude", FieldTypes.DoubleType));
            //this.AddField(new IndexField("latitude", FieldTypes.DoubleType));
            this.AddField(new IndexField("geo", FieldTypes.GeoType));

            this.AddField(new StoredField("thumbnail_url"));
            this.AddField(new StoredField("bbox"));

            this.PrimaryFields = new string[] { "suggested_text", "subtext", "category" };
            this.PrimaryFieldsEncodeCharacters = encodeCharacters != null && encodeCharacters.Length > 0 ?
                encodeCharacters : null;

            this.PrimaryFieldsPhonetics = algorithm;
        }
    }
}
