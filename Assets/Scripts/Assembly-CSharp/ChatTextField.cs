using System;
using UnityEngine;

public class ChatTextField : AvMain
{
	public TField tfChat;

	public static ChatTextField instance;

	public static bool isShow;

	private iCommand cmdChat;

	public static ChatTextField gI()
	{
		if (instance != null)
		{
			return instance;
		}
		return instance = new ChatTextField();
	}

	public void setChat()
	{
		isShow = !isShow;
		if (isShow)
		{
			tfChat.setPoiter();
		}
	}

	public override void commandTab(int index, int subIndex)
	{
		switch (index)
		{
		case 0:
			GameCanvas.clearAll();
			tfChat.setText("");
			isShow = false;
			if (!GameCanvas.isTouch)
			{
				tfChat.setFocus(isFocus: true);
			}
			break;
		case 1:
			sendChat();
			break;
		}
	}

	protected ChatTextField()
	{
		tfChat = new TField();
		tfChat.isChangeFocus = false;
		tfChat.setFocus(isFocus: true);
		init();
		tfChat.x = (MotherCanvas.w - tfChat.width) / 2;
		if (GameMidlet.DEVICE == 2)
		{
			tfChat.x = 10;
		}
		tfChat.setMaxTextLenght(70);
		tfChat.setStringNull(T.chat); 
			left = new iCommand(T.close, 0);
			center = new iCommand(T.chat, 1);
			right = tfChat.setCmdClear(); 
	 
		 
	}

	public void init()
	{
		tfChat.y = MotherCanvas.h - iCommand.hButtonCmdNor - tfChat.height - 15;
		tfChat.width = MotherCanvas.w - TField.xDu * 2 - 20;
		 
    }

	public void keyPressed(int keyCode)
	{
		tfChat.keyPressed(keyCode);
	}

	public override void updatekey()
	{
		tfChat.update();
		base.updatekey();
	}

	public override void paint(mGraphics g)
	{
		try
		{
			base.paint(g);
			tfChat.paint(g);
			//cmdChat.paint(g, cmdChat.xCmd, cmdChat.yCmd);
		}
		catch(Exception ex)
		{
			Debug.LogException(ex);
		}
	}

	public override void updatePointer()
	{
		tfChat.updatePointer();
		base.updatePointer();
	}

	public void sendChat()
	{
		if (tfChat.getText().Length > 0)
		{
			string text = tfChat.getText();
			if (text.StartsWith("/"))
			{
				if (text.Equals("/an") || text.Equals("/annguoi"))
				{
					GameScreen.isShowNhanVat = !GameScreen.isShowNhanVat;
					Interface_Game.addInfoPlayerNormal("Ẩn người chơi khác: " + (!GameScreen.isShowNhanVat ? "Bật" : "Tắt"), mFont.tahoma_7_yellow);
					tfChat.setText("");
					isShow = false;
					return;
				}
				else if (text.Equals("/anhu") || text.Equals("/anhieuung"))
				{
					GameScreen.isShowSkillPlayer = !GameScreen.isShowSkillPlayer;
					Interface_Game.addInfoPlayerNormal("Ẩn hiệu ứng & sát thương: " + (!GameScreen.isShowSkillPlayer ? "Bật" : "Tắt"), mFont.tahoma_7_yellow);
					tfChat.setText("");
					isShow = false;
					return;
				}
			}
			GameScreen.player.strChatPopup = text;
			GlobalService.gI().chatPopup(text);
			tfChat.setText("");
		}
		if (GameCanvas.isTouch)
		{
			isShow = false;
		}
	}
}
