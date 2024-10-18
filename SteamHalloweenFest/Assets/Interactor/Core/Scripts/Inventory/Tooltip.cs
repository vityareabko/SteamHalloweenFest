using UnityEngine;
using UnityEngine.UI;

namespace razz
{
    public class Tooltip : MonoBehaviour
    {
		public Text tooltipText;

		public Color titleColor;
		public Color textColor;

		private Item _item;
		private string _titleColorCode;
		private string _rarityColorCode;
		private string _descColorCode;


		public void Activate(Item item)
		{
			if (!tooltipText || !ItemDatabase.Instance) return;

			_item = item;
			ConstructDataString(ItemDatabase.Instance.GetItemDataByIndex(item.dbIndex));
			gameObject.SetActive(true);
		}

		public void Deactivate()
		{
			gameObject.SetActive(false);
		}

		public void ConstructDataString(ItemData item)
		{
			_titleColorCode = ColorUtility.ToHtmlStringRGB(titleColor);
			_rarityColorCode = ColorUtility.ToHtmlStringRGB(ItemDatabase.rarityColors[(int)item.Rarity]);
			_descColorCode = ColorUtility.ToHtmlStringRGB(textColor);

			tooltipText.text = $"<color=#{_titleColorCode}><b>{item.Title}</b></color>\n\n" +
							  $"<color=#{_rarityColorCode}>{item.Rarity}</color>\n\n" +
							  $"<color=#{_descColorCode}>" +
							  $"{item.Description}\n\n" +
							  $"Condition: {item.Value}" +
							  $"</color>";
		}
	}
}
