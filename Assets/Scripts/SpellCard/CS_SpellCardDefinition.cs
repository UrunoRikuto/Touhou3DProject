using UnityEngine;

/// <summary>
/// 複数の弾幕バリエーション(CS_BarragePatternプレハブ)を、いつ・どんな角度で
/// 再生するかという情報(CS_SpellCardEntryの配列)としてまとめたアセット。
///
/// バリエーション本体(弾の出し方)はプレハブ側の責務、こちらは「組み合わせ方」だけを持つ
/// 純粋なデータアセット。CS_SpellCardComposerWindowで編集し、CS_SpellCardPlayerが
/// このアセットを読んで実際にInstantiate・再生する。
///
/// Assets/Data/SpellCards/ 以下に保存することを想定(CS_SpellCardComposerWindowの
/// 「新規スペルカードを作成」がデフォルトでこのフォルダを提案する)。
/// </summary>
[CreateAssetMenu(fileName = "NewSpellCard", menuName = "東方3D弾幕/Spell Card Definition")]
public class CS_SpellCardDefinition : ScriptableObject
{
    public CS_SpellCardEntry[] entries = new CS_SpellCardEntry[0];
}
