#define AS_SINGLETON
using UnityEngine;
using System.Collections.Generic;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Kayac
{
	public class SoundManager : MonoBehaviour
	{
#if AS_SINGLETON
		public static SoundManager Instance { get; private set; }
#endif
		[SerializeField] int seChannelCount = 4;
		[SerializeField] float masterVolume;
		[SerializeField] float bgmVolume;
		[SerializeField] float seVolume;
		[SerializeField] bool muted;
		[SerializeField] string assetRootPath;
		[SerializeField] float silenceDb = -80f;
		[SerializeField] int clipCacheSize = 32;
		[SerializeField] bool autoUpdate = true;

		public bool Muted { get { return muted; } set { muted = value; } }
		public float MasterVolume { get { return masterVolume; } set { masterVolume = value; } }
		public float SeVolume { get { return SeVolume; } set { SeVolume = value; } }
		public float BgmVolume { get { return BgmVolume; } set { BgmVolume = value; } }
		public bool BgmPlaying
		{
			get
			{
				return bgmChannels[0].Playing || bgmChannels[1].Playing;
			}
		}

		public bool BgmLooping
		{
			get
			{
				var channel = bgmChannels[1 - nextBgmChannelIndex];
				return channel.Playing && channel.Looping;
			}
		}

		public string BgmName
		{
			get
			{
				var channel = bgmChannels[1 - nextBgmChannelIndex];
				return channel.ClipName;
			}
		}

		public bool IsClipLoading(string name)
		{
			return clipManager.IsLoading(name);
		}

		public bool IsAnyClipLoading(IEnumerable<string> names)
		{
			foreach (var name in names)
			{
				if (IsClipLoading(name))
				{
					return true;
				}
			}
			return false;
		}

		void Awake()
		{
			// BGM生成
			bgmChannels = new Channel[2];
			bgmChannels[0] = CreateChannel("BgmChannel0");
			bgmChannels[1] = CreateChannel("BgmChannel1");
			// SE生成
			seChannels = new Channel[seChannelCount];
			for (int i = 0; i < seChannelCount; i++)
			{
				seChannels[i] = CreateChannel("SeChannel" + i.ToString());
			}
			clipManager = new ClipManager(assetRootPath, clipCacheSize);
#if AS_SINGLETON
			Instance = this;
#endif
		}

		Channel CreateChannel(string channelName)
		{
			var go = new GameObject(channelName);
			go.transform.SetParent(transform, false);
			var source = go.AddComponent<AudioSource>();
			go.AddComponent<AudioReverbFilter>().reverbPreset = AudioReverbPreset.Hallway;
			var ret = new Channel(source);
			return ret;
		}

		void OnDestroy()
		{
			Dispose();
#if AS_SINGLETON
			Instance = null;
#endif
		}

		public void Dispose()
		{
			if (bgmChannels != null)
			{
				bgmChannels[0].Dispose();
				bgmChannels[1].Dispose();
				bgmChannels = null;
			}
			if (seChannels != null)
			{
				for (int i = 0; i < seChannels.Length; i++)
				{
					seChannels[i].Dispose();
				}
				seChannels = null;
			}
			// クリップ破棄
			if (clipManager != null)
			{
				clipManager.Dispose();
				clipManager = null;
			}
		}

		public void PlayBgm(
			string name,
			float volume = 0f,
			bool loop = true,
			float fadeOutDuration = 0.5f, // この時間かけて音量が0になる
			float fadeInDuration = 0f, // この時間かけて音量がvolumeまで上がる
			float overlapDuration = 0f, // 2音が重なる時間。最大fadeOutDuration。負なら間隔が空く。
			bool ignoreSame = true)
		{
			Debug.Assert(!string.IsNullOrEmpty(name));
			if (Muted || ((masterVolume + bgmVolume + volume) <= silenceDb))
			{
				return;
			}

			PlayBgmInternal(
				name,
				volume,
				loop,
				fadeOutDuration,
				fadeInDuration,
				overlapDuration,
				ignoreSame);
		}

		// 現在の曲の終了後に続けて再生する
		public void ReserveBgm(
			string name,
			float volume = 0f,
			bool loop = true,
			float fadeInDuration = 0f, // この時間かけて音量がvolumeまで上がる
			bool ignoreSame = true)
		{
			Debug.Assert(!string.IsNullOrEmpty(name));
			if (Muted || ((masterVolume + bgmVolume + volume) <= silenceDb))
			{
				return;
			}

			var oldBgm = bgmChannels[1 - nextBgmChannelIndex];
			Debug.Assert(!oldBgm.Playing || !oldBgm.Looping, "現在再生中のBGMはループ指定なので、永遠に終わらない。");

			PlayBgm(
				name,
				volume,
				loop,
				0f,
				fadeInDuration,
				OverlapDurationUntilEnd,
				ignoreSame);
		}

		void PlayBgmInternal( // 無音を鳴らす要求をPlayBgmでは蹴りたいが、再生停止は無音を開始することで行うので、Internalとして分ける
			string clipName,
			float volume,
			bool loop,
			float fadeOutDuration,
			float fadeInDuration,
			float overlapDuration,
			bool ignoreSame)
		{
			if (ignoreSame && (clipName == BgmName))
			{
				return;
			}

			if (overlapDuration <= OverlapDurationUntilEnd)
			{
				bgmReserved = true; // 終了を待って再生
				overlapDuration = 0f;
				fadeOutDuration = 0f;
			}
			else // 終了処理開始
			{
				bgmReserved = false; // 通常の経路で再生されたら予約は消す
				var oldBgm = bgmChannels[1 - nextBgmChannelIndex];
				if (oldBgm.Busy)
				{
					oldBgm.Stop(fadeOutDuration, silenceDb);
				}
				else
				{
					fadeOutDuration = 0f; // FadeOut不要
					if (overlapDuration > 0f)
					{
						overlapDuration = 0f;
					}
				}
			}

			if (!string.IsNullOrEmpty(clipName))
			{
				var clip = clipManager.Load(clipName);
				var delay = fadeOutDuration - overlapDuration; // overlapが長いほどdelayは小さくなる。overlapが負ならdelayは極めて大きくなる。
				Debug.Assert(delay >= 0f);
				// 再生開始
				bgmChannels[nextBgmChannelIndex].RequestPlay(
					clip,
					volume,
					masterVolume + bgmVolume,
					loop,
					1f, // pitch
					delay,
					fadeInDuration,
					startWithPaused: bgmReserved, // 予約再生であればポーズ状態で準備
					silenceDb: silenceDb);
			}
			nextBgmChannelIndex = 1 - nextBgmChannelIndex;
		}

		// stopSame=trueならすでに鳴っている同名の音を止めて新しく鳴らし直す
		public void PlaySe(
			string name,
			float volume = 0f,
			bool loop = false,
			bool stopSame = true,
			float pitch = 1f,
			float delay = 0f)
		{
			Debug.Assert(!string.IsNullOrEmpty(name));

			if (Muted || ((masterVolume + seVolume + volume) <= silenceDb))
			{
				return;
			}

			// 再利用すべきものを探し、同時に空きも探す。プレイヤーの数はたかだか数十なので線形検索でいい。
			int channelIndex = -(int.MaxValue);
			DateTime earliestEndTime = DateTime.MaxValue;
			int stoppedIndex = -(int.MaxValue);
			for (int i = 0; i < seChannels.Length; i++)
			{
				var channel = seChannels[i];
				if (channel.Busy)
				{
					// 再利用判定
					Debug.Assert(channel.Clip != null);
					if (stopSame && name.Equals(channel.ClipName)) // 同じものを止めるフラグ
					{
						channel.StopImmediately();
						channelIndex = i;
						break;
					}
					var endTime = channel.EstimatedEndTime;
					if (endTime < earliestEndTime)
					{
						earliestEndTime = endTime;
						stoppedIndex = i;
					}
				}
				else if (channelIndex < 0)
				{
					channelIndex = i;
				}
			}

			// 空いてない場合は古いものを止める
			if ((channelIndex < 0) && (stoppedIndex >= 0))
			{
				channelIndex = stoppedIndex;
			}

			if (channelIndex >= 0)
			{
				var clip = clipManager.Load(name);
				seChannels[channelIndex].RequestPlay(
					clip,
					volume,
					masterVolume + seVolume,
					loop,
					pitch,
					delay,
					fadeDuration: 0f,
					startWithPaused: false,
					silenceDb: silenceDb);
			}
		}

		public void StopBgm(float fadeDuration = 0.5f)
		{
			PlayBgmInternal(null, 0f, false, fadeDuration, 0f, 0f, false); // 無音を再生開始する扱い
		}

		public void StopSe(string name, float fadeDuration = 0f)
		{
			foreach (var channel in seChannels)
			{
				if (channel.ClipName == name)
				{
					channel.Stop(fadeDuration, silenceDb);
				}
			}
		}

		public void StopAllLoopSe()
		{
			foreach (var channel in seChannels)
			{
				if (channel.Looping)
				{
					channel.StopImmediately();
				}
			}
		}

		public void StopAllSe()
		{
			foreach (var channel in seChannels)
			{
				channel.StopImmediately();
			}
		}

		public void StopAll(bool immediately = false)
		{
			if (immediately)
			{
				StopBgm(fadeDuration: 0f);
			}
			else
			{
				StopBgm();
			}
			StopAllSe();
		}

		void Update()
		{
			if (autoUpdate)
			{
				ManualUpdate(Time.deltaTime);
			}
		}

		public void ManualUpdate(float deltaTime)
		{
			clipManager.ManualUpdate(deltaTime);

			// 全Player更新
			var volumeOffset = masterVolume + seVolume;
			foreach (var channel in seChannels)
			{
				channel.ManualUpdate(deltaTime, volumeOffset, silenceDb);
			}
			volumeOffset = masterVolume + bgmVolume;
			foreach (var channel in bgmChannels)
			{
				channel.ManualUpdate(deltaTime, volumeOffset, silenceDb);
			}

			// 予約再生
			if (bgmReserved)
			{
				if (!bgmChannels[nextBgmChannelIndex].Busy) // 再生完了してる
				{
					bgmChannels[1 - nextBgmChannelIndex].EndPause();
					bgmReserved = false; // 予約を果たした
				}
			}
		}

		public void UnloadUnused()
		{
			clipManager.UnloadUnused();
		}

		// Non-Public --------------------
		Channel[] seChannels;
		Channel[] bgmChannels;
		int nextBgmChannelIndex;
		ClipManager clipManager;
		const float OverlapDurationUntilEnd = float.NegativeInfinity;
		bool bgmReserved;

		// 振幅率[0,1] → dB
		static float ToDecibel(float value)
		{
			float decibel = -(float.MaxValue);
			if (value > 0f)
			{
				decibel = 20f * Mathf.Log10(value);
			}
			return decibel;
		}

		// dB → 振幅率[0, 1]
		static float FromDecibel(float decibel)
		{
			var value = Mathf.Pow(10f, decibel / 20f);
			return value;
		}

		// 内部クラス群
		class Channel : IDisposable
		{
			public bool Playing { get; private set; }
			public bool Busy
			{
				get
				{
					return playRequested || Playing;
				}
			}
			public string ClipName
			{
				get
				{
					return (Clip != null) ? Clip.Name : null;
				}
			}
			public Clip Clip { get; private set; }
			public Exception Exception { get; private set; }
			public float BaseVolume { private get; set; }
			public float Volume { get; private set; }
			public DateTime EstimatedEndTime { get; private set; }

			public bool Looping
			{
				get
				{
					return source.loop;
				}
			}

			public Channel(AudioSource source)
			{
				this.source = source;
			}


			public void Dispose()
			{
				// 参照カウント--
				if (Clip != null)
				{
					Clip.Release();
					Clip = null;
				}
				if (source != null)
				{
					UnityEngine.Object.Destroy(source.gameObject);
					source = null;
				}
			}

			public void RequestPlay(
				Clip clip,
				float baseVolume,
				float volumeModifier,
				bool loop,
				float pitch,
				float delay,
				float fadeDuration,
				bool startWithPaused,
				float silenceDb)
			{
				if (clip.Exception != null) // エラーこいてればスルー
				{
					return;
				}
				Debug.Assert(source != null);
				// まず止める
				StopImmediately();
				// パラメータを記録
				Volume = BaseVolume = baseVolume;
				source.loop = loop;
				source.pitch = pitch;
				// クリップを保持
				Clip = clip;
				// 参照カウント++
				Clip.Refer();
				// 再生要求フラグセット
				playRequested = true;
				if (fadeDuration == 0f)
				{
					fadeSpeed = 0f;
					fadedVolume = baseVolume;
				}
				else
				{
					fadeSpeed = (baseVolume - silenceDb) / fadeDuration;
					fadedVolume = silenceDb;
				}
				source.volume = FromDecibel(fadedVolume + volumeModifier);
				this.delay = delay;
				paused = startWithPaused;

				// すでにロード済みの場合に備えて一回Update
				ManualUpdate(0f, volumeModifier, silenceDb);
				if (loop)
				{
					EstimatedEndTime = DateTime.MaxValue;
				}
				else
				{
					EstimatedEndTime = DateTime.Now + TimeSpan.FromSeconds(clip.Length);
				}
			}

			public void EndPause()
			{
				paused = false;
			}

			public void ManualUpdate(float deltaTime, float volumeModifier, float silenceDb)
			{
				if (paused) // ポーズ中なら抜ける
				{
					return;
				}
				// 再生要求中ならロードを監視して完了していれば再生する
				if (playRequested)
				{
					TryPlay(deltaTime);
				}
				// 再生中なら終了を検出する
				if (Playing)
				{
					if (fadeSpeed != 0f) // フェードインアウト
					{
						fadedVolume += fadeSpeed * deltaTime;
						fadedVolume = Mathf.Clamp(fadedVolume, silenceDb, 0f);
						if ((fadeSpeed < 0f) && (fadedVolume <= silenceDb)) // baseボリュームがほぼゼロになったら止める
						{
							StopImmediately();
						}
					}

					// AudioSourceが再生中と言っていて、時刻が非ゼロなら実際に再生したとみなす
					if (source.isPlaying)
					{
						var newVolume = fadedVolume + volumeModifier;
						if (Volume != newVolume)
						{
							var newVolumeLinear = SoundManager.FromDecibel(newVolume);
							Volume = newVolume;
							source.volume = newVolumeLinear; // 動的ボリューム変化に追随
						}
						if (!actuallyStarted && (source.time != 0f))
						{
							actuallyStarted = true;
						}
					}
					// AudioSourceが再生中でないと言っていて、実際に再生したことがあるなら、止める。
					// 実際に再生したことを確認していない間は信用しない
					else if (actuallyStarted) // _source.isPlaying == falseな状態
					{
						StopImmediately();
					}
				}
			}

			public void StopImmediately()
			{
				Debug.Assert(source != null);
				source.Stop();
				// 参照カウント--
				if (Clip != null)
				{
					Clip.Release();
					Clip = null;
				}
				playRequested = false;
				actuallyStarted = false;
				Playing = false;
			}

			public void Stop(float fadeDuration, float silenceDb)
			{
				fadeSpeed = (silenceDb - BaseVolume) / fadeDuration;
			}

			// Non-Public -------------
			AudioSource source;
			bool playRequested;
			// Resources経由の再生だと開始時にisPlayingが数フレームfalseになるため、
			// それで再生終了とすると間違う。そこで、実際にtime>0を経由したことを確認する。
			bool actuallyStarted;
			float delay; // 遅延。Updateごとにdecrementして0になったら発音
			float fadeSpeed; // プラスならFadeIn、マイナスならFadeOut
			float fadedVolume; // Fadeを加味したボリューム
			bool paused;

			void TryPlay(float deltaTime)
			{
				Debug.Assert(playRequested);
				Debug.Assert(!Playing);
				Debug.Assert(Clip != null);
				var currentDelay = delay;
				delay -= deltaTime;
				if (currentDelay > 0f)
				{
					return;
				}

				if (!Clip.Loading)
				{
					// 要求を果たした(clipがエラーで鳴らせなくてもかまわない)
					playRequested = false;
					// AudioClipでなかった場合nullが返り得る
					var audioClip = Clip.AudioClip;
					if (audioClip != null)
					{
						source.clip = audioClip;
						source.Play();
						Playing = true;
					}
				}
			}
		}

		class Clip : IDisposable
		{
			int referenceCount;
			ResourceRequest loadRequest;
			public string Name { get; private set; }
			public Exception Exception { get; private set; }
			public LinkedListNode<Clip> ListNode { get; set; }
			public AudioClip AudioClip { get; private set; }

			public float Length
			{
				get
				{
					return (AudioClip != null) ? AudioClip.length : 0f;
				}
			}

			public bool Loading
			{
				get
				{
					// AudioClipがなく、かつ、エラーでない場合、まだ読んでいる
					return (AudioClip == null) && (Exception == null);
				}
			}

			public bool Referenced
			{
				get
				{
					return referenceCount > 0;
				}
			}

			public Clip(ResourceRequest loadRequest, string name)
			{
				this.loadRequest = loadRequest;
				Name = name;
			}

			public void Dispose()
			{
				// ロード待ちの間に破棄してはならない
				Debug.Assert(!Loading);
				// 参照カウントが残っているのに破棄してはならない
				Debug.Assert(referenceCount == 0);
				if ((loadRequest != null) && loadRequest.isDone && (loadRequest.asset != null))
				{
					Resources.UnloadAsset(loadRequest.asset);
				}
				loadRequest = null;
			}

			public void ManualUpdate(float deltaTime)
			{
				if (!Loading)
				{
					// 何もしない
				}
				else if (loadRequest == null)
				{
					// 何もしない
				}
				else if (!loadRequest.isDone)
				{
					// 何もしない
				}
				else
				{
					var asset = loadRequest.asset;
					if (asset == null)
					{
						Exception = new Exception("can't load asset name=" + Name);
						Debug.LogException(Exception);
					}
					else
					{
						AudioClip = asset as AudioClip;
						if (AudioClip == null)
						{
							Exception = new Exception("the asset is not AudioClip type=" + asset.GetType().Name);
							Debug.LogException(Exception);
						}
					}
				}
			}

			public void Refer()
			{
				referenceCount++;
			}

			public void Release()
			{
				referenceCount--;
			}
		}

		class ClipManager : IDisposable
		{
			public int Count { get { return (clips != null) ? clips.Count : 0; } }
			public ClipManager(string rootPath, int cacheSize)
			{
				this.rootPath = rootPath;
				if (!string.IsNullOrEmpty(rootPath))
				{
					this.rootPath += "/";
				}
				clips = new Dictionary<string, Clip>();
				rankedClips = new LinkedList<Clip>();
				this.cacheSize = cacheSize;
			}

			public void UnloadUnused()
			{
				Debug.Assert(rankedClips != null);
				Debug.Assert(clips != null);
				var cur = rankedClips.First;
				while (cur != null)
				{
					var next = cur.Next;
					var clip = cur.Value;
					if (!clip.Referenced && !clip.Loading)
					{
						Unload(clip);
					}
					cur = next;
				}
			}

			public void TryUnload(string name)
			{
				if (clips.ContainsKey(name))
				{
					var clip = clips[name];
					clip.Release();
					if (!clip.Referenced && !clip.Loading)
					{
						Unload(clip);
					}
				}
			}

			public bool IsLoading(string name)
			{
				bool ret = false;
				if (clips.ContainsKey(name))
				{
					var clip = clips[name];
					if (clip.Loading) // ロード中なら
					{
						ret = true;
					}
				}
				return ret;
			}

			public float GetClipLength(string name)
			{
				float ret = 0f;
				if (clips.ContainsKey(name))
				{
					var clip = clips[name];
					ret = clip.Length;
				}
				return ret;
			}

			public void Dispose()
			{
				if (rankedClips != null)
				{
					var cur = rankedClips.First;
					while (cur != null)
					{
						cur.Value.Dispose();
						cur = cur.Next;
					}
				}
				rankedClips = null;
				clips = null;
			}

			public void ManualUpdate(float deltaTime)
			{
				Debug.Assert(rankedClips != null);
				var cur = rankedClips.First;
				while (cur != null)
				{
					var clip = cur.Value;
					clip.ManualUpdate(deltaTime);
					cur = cur.Next;
				}
				// キャッシュ更新
				cur = rankedClips.Last;
				while ((rankedClips.Count > cacheSize) && (cur != null))
				{
					var prev = cur.Previous;
					var clip = cur.Value;
					if (!clip.Referenced && !clip.Loading)
					{
						Unload(clip);
					}
					cur = prev;
				}
			}

			public Clip Load(string name)
			{
				Clip ret;
				if (clips.ContainsKey(name))
				{
					ret = clips[name];
					rankedClips.Remove(ret.ListNode);
					rankedClips.AddFirst(ret.ListNode);
				}
				else
				{
					var path = rootPath + name;
					var request = Resources.LoadAsync(path);
					ret = new Clip(request, name);

					clips.Add(name, ret);
					// リストの先頭につなぐ
					rankedClips.AddFirst(ret);
					ret.ListNode = rankedClips.First;
				}
				return ret;
			}

			// Non-Public -------------------
			Dictionary<string, Clip> clips;
			// 最近使ったものほど先頭に近い場所に来るように並べたリスト
			LinkedList<Clip> rankedClips;
			int cacheSize;
			string rootPath;

			void Unload(Clip clip)
			{
				clips.Remove(clip.Name);
				rankedClips.Remove(clip.ListNode);
				clip.Dispose();
			}
		}
#if UNITY_EDITOR
		[CustomEditor(typeof(SoundManager))]
		public class Inspector : Editor
		{
			string clipName;
			float fadeOutDuration = 2f;
			float fadeInDuration = 1f;
			float pitch = 1.2f;
			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();
				var self = target as SoundManager;
				if (self.clipManager != null)
				{
					clipName = EditorGUILayout.TextField("clipName", clipName);
					fadeOutDuration = EditorGUILayout.FloatField("fadeOut", fadeOutDuration);
					fadeInDuration = EditorGUILayout.FloatField("fadeIn", fadeInDuration);
					pitch = EditorGUILayout.FloatField("pitch", pitch);
					if (GUILayout.Button("PlayBGM"))
					{
						self.PlayBgm(clipName, 0f, true, fadeOutDuration, fadeInDuration, fadeOutDuration, true);
					}
					if (GUILayout.Button("PlaySE"))
					{
						self.PlaySe(clipName, 0f, false, true, pitch, 0f);
					}
					if (GUILayout.Button("Stop"))
					{
						self.StopAll();
					}
					GUILayout.Label("clipCache: " + self.clipManager.Count);
					ShowChannel("bgm0", self.bgmChannels[0]);
					ShowChannel("bgm1", self.bgmChannels[1]);
					for (int i = 0; i < self.seChannels.Length; i++)
					{
						ShowChannel("se" + i, self.seChannels[i]);
					}
					EditorUtility.SetDirty(target);
				}
			}

			void ShowChannel(string label, Channel channel)
			{
				GUILayout.Label(label + ": " + (channel.Playing ? channel.ClipName : "") + " " + channel.Volume.ToString("F1"));
			}
		}
#endif
	}
}
