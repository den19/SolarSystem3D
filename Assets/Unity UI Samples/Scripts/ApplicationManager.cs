using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ApplicationManager : MonoBehaviour {
	

	public void Quit () 
	{
		#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
		
		#elif UNITY_ANDROID
		try
		{
			// Получаем текущую Activity приложения в Android
			using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
			{
				using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
				{
					// finishAndRemoveTask() закрывает все Activity и удаляет приложение из списка запущенных (Recents)
					currentActivity.Call<bool>("finishAndRemoveTask");
				}
			}
		}
		catch (System.Exception e)
		{
			Debug.LogError("Ошибка при нативном закрытии на Android: " + e.Message);
			Application.Quit(); // В случае непредвиденной ошибки используем стандартный метод
		}
		
		// Мгновенно убиваем системный процесс приложения, полностью выгружая его из оперативной памяти
		System.Diagnostics.Process.GetCurrentProcess().Kill();

		#else
		// Стандартный выход для ПК и других платформ
		Application.Quit();
		#endif
	}

	public void StartGame()
	{
		GameState.isPaused = true;
	}
}
