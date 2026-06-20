using System;

public class UpdateImageScreen : MainScreen
{
	public static int maxNum;

	public static int curNum;

	private int wpaint = -1;

	private int maxwPaint;

	private int x;

	private int y;

	private int hpaint;

	private long timeBegin;

	public static sbyte statusUpdate = 0;

	public const sbyte CONNECT = 0;

	public const sbyte FAIL = 1;

	public const sbyte LOADING = 2;

	public const sbyte LOADING_OK = 3;

	public static string strPaint = "";

	private mImage imglogo;

	private mImage imgsea;

	private mImage imgsky;

	private mImage imgloading1;

	private mImage imgloading2;

	private mImage imgloading3;

	private int wSky;

	private int wSea;

	private int hSky;

	private int hSea;

	public UpdateImageScreen()
	{
		maxwPaint = 122;
		hpaint = 14;
		x = MotherCanvas.hw;
		y = MotherCanvas.h / 5 * 4 - 7;
		beginLoadImage();
		timeBegin = mSystem.currentTimeMillis();
		statusUpdate = 0;
		setmNamePaint(T.pleaseWaiting);
		loadImage();
	}

	public void loadImage()
	{
		if (GameCanvas.language == 1)
		{
			imglogo = mImage.createImage("/new/lgv_e.png");
		}
		else
		{
			imglogo = mImage.createImage("/new/lgv.png");
		}
		imgloading1 = mImage.createImage("/new/koload.png");
		imgloading2 = mImage.createImage("/new/load.png");
		imgloading3 = mImage.createImage("/new/thuyen.png");
		imgsea = mImage.createImageAll("/up0.png");
		imgsky = mImage.createImageAll("/up1.png");

		if (imgsky != null && imgsky.image != null)
		{
			wSky = mImage.getImageWidth(imgsky.image);
			hSky = mImage.getImageHeight(imgsky.image);
		}
		else
		{
			wSky = 100;
			hSky = 100;
		}

		if (imgsea != null && imgsea.image != null)
		{
			wSea = mImage.getImageWidth(imgsea.image);
			hSea = mImage.getImageHeight(imgsea.image);
		}
		else
		{
			wSea = 100;
			hSea = 100;
		}
	}

	public override void paint(mGraphics g)
	{
		g.setColor(6014975);
		g.fillRect(0, 0, MotherCanvas.w, MotherCanvas.h / 2);
		g.setColor(16765819);
		g.fillRect(0, MotherCanvas.h / 2, MotherCanvas.w, MotherCanvas.h / 2);
		for (int i = 0; i <= MotherCanvas.w / wSky; i++)
		{
			g.drawImage(imgsky, i * wSky, MotherCanvas.hh - hSky / 2, 0);
		}
		for (int j = 0; j <= MotherCanvas.w / wSea; j++)
		{
			g.drawImage(imgsea, j * wSea, MotherCanvas.hh + hSky / 2, 0);
		}
		if (imglogo != null)
		{
			g.drawImage(imglogo, MotherCanvas.hw, MotherCanvas.h / 5, 3);
		}
		g.setColor(0);
		g.drawString(strPaint, MotherCanvas.hw, y - 20 + 7, 2);
		if (statusUpdate == 2 || statusUpdate == 3)
		{
			g.drawImage(imgloading1, x - 61, y - 8, 0);
			if (wpaint >= 0)
			{
				g.drawRegion(imgloading2, 0, 0, wpaint, 16, 0, x - 61, y - 8, 0);
			}
			int num = wpaint;
			if (num < 10)
			{
				num = 10;
			}
			if (num > maxwPaint - 12)
			{
				num = maxwPaint - 12;
			}
			g.drawString(curNum + " / " + maxNum, MotherCanvas.hw, y + 4, 2);
			g.drawImage(imgloading3, x - 60 + num, y, 3);
		}
	}

	public override void update()
	{
		if (maxNum > 0)
		{
			wpaint = maxwPaint * curNum / maxNum;
			if (wpaint > maxwPaint)
			{
				wpaint = maxwPaint;
			}
		}
		if (statusUpdate == 0 && (GameCanvas.timeNow - timeBegin) / 1000 > 15)
		{
			if (GameCanvas.indexdownload == 0)
			{
				Session_ME.gI().close();
				GameCanvas.indexdownload++;
				GameCanvas.connectDownload();
				GlobalService.gI().Request_Image_Android();
				timeBegin = GameCanvas.timeNow;
			}
			else
			{
				setmNamePaint(T.disconnectUpdateImage);
				statusUpdate = 1;
			}
		}
		if (statusUpdate == 3 && SaveImageRMS.vecSaveImageAndroid.size() == 0)
		{
			GameCanvas.instance.beginGame();
			saveVer();
		}
	}

	public override void updatePointer()
	{
		if (statusUpdate == 1 && GameCanvas.isPointerDown)
		{
			Session_ME.gI().close();
			GameCanvas.indexdownload++;
			GameCanvas.connectDownload();
			GlobalService.gI().Request_Image_Android();
			timeBegin = GameCanvas.timeNow;
			statusUpdate = 0;
			setmNamePaint(T.pleaseWaiting);
		}
		base.updatePointer();
	}

	public static void setValueUpdate(int cur, int max)
	{
		curNum = cur;
		if (max >= 0)
		{
			maxNum = max;
		}
		statusUpdate = 2;
	}

	public static void setmNamePaint(string str)
	{
		strPaint = str;
	}

	public void saveVer()
	{
		ByteArrayOutputStream byteArrayOutputStream = new ByteArrayOutputStream();
		DataOutputStream dataOutputStream = new DataOutputStream(byteArrayOutputStream);
		try
		{
			dataOutputStream.writeUTF("1.2.9");
			CRes.saveRMS("Main_Load_Image_Android_OK", byteArrayOutputStream.toByteArray());
			dataOutputStream.close();
		}
		catch (Exception)
		{
		}
	}

	public void beginLoadImage()
	{
		Session_ME.gI().close();
		GameCanvas.connectDownload();
		GlobalService.gI().Request_Image_Android();
	}
}
