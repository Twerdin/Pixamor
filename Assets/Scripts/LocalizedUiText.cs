using TMPro;
namespace Pixamor
{
    // Kaynak metni saklarız: dili değiştirince mevcut başlıklar da anında yenilenir.
    public class LocalizedUiText : TextMeshProUGUI
    {
        private string source;
        public override string text { get => base.text; set { source=value;RefreshLanguage(); } }
        private void RefreshLanguage() { base.text=GameLanguage.Translate(source); }
        protected override void OnEnable() { base.OnEnable();GameLanguage.Changed+=RefreshLanguage;if(source!=null)RefreshLanguage(); }
        protected override void OnDisable() { GameLanguage.Changed-=RefreshLanguage;base.OnDisable(); }
    }
}
