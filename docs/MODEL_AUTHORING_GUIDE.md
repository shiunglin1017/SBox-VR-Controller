# 模型交付與設定建議（Model Authoring Guide）

這份文件給「要把 FBX / VMDL / animgraph 交給 SBox-VR-Controller 專案使用」
的模型製作者參考。整套 VR 控制（IK、抓取、後座、掛回）建立在 s&box 官方
管線上，所以模型交付的標準幾乎跟 Citizen 對齊。

## 中文速查（zh-TW）

| 主題 | 必須做到 |
| --- | --- |
| 檔案 | binary FBX；T-pose 或 A-pose；單位 cm；面向專案 forward |
| 骨架 | 命名與 Citizen 對齊（`hand_L / arm_lower_L / clavicle_L / ...`）或附 `.skmap` |
| 動畫 | 至少一個 `AnimBindPose` 或 reference 動畫（避免 morph / IK / attachment 被 strip） |
| animgraph | `Base Model = models/citizen_human/citizen_human_male.vmdl`（最簡單，自帶 `ik.hand_left/right`、`ik.foot_left/right`） |
| ModelDoc | `SkinnedModelRenderer.CreateAttachments = true`；attachment 名 `weapon_hold`、`back_rifle`、`hip_holster_l`、`hip_holster_r` |
| Hitbox | `head` / `body` / `arm` / `leg`，tag 內放傷害倍率（`BulletProjectile` 會讀 float） |

## 1. 必須交付的內容

| 項目 | 說明 |
| --- | --- |
| `*.fbx`（必） | binary FBX，不要 ASCII。Blender 使用者只能讀 binary |
| 動畫 FBX（建議） | 多動畫請用 FBX `take` 機制，或拆多個 FBX 給 ModelDoc 的 `Add Simple Animations` 引入 |
| 材質 / 貼圖 | albedo / normal / roughness / metallic 等；如果使用 morph，材質要勾 `Morph Enabled` |
| Mapping profile（可選） | 若骨名與 Citizen 不一致，附一份 `.skmap`（`SkeletonMappingProfile`）或骨對表 |

## 2. 姿勢與比例

- **T-pose 或 A-pose**：兩種都可以，但要一致。手心方向需與 Citizen 同向
  （手心朝下、手指朝前）。
- **面向**：模型 forward 與 s&box 匯入方向一致。
- **比例**：身高 1.6 - 1.9m；不要做超長手或誇張比例，IK 接得上但 VR 內看
  起來會怪。
- **單位**：來源檔案用 cm，匯入時透過 ModelDoc 的 `ScaleAndMirror`（值
  0.3937）轉成 inch；參考 [`citizen-characters.md`](https://docs.facepunch.com/s/sbox-dev/doc/citizen-characters)
  最後一段。

## 3. 骨架命名

最理想：直接使用 Citizen 命名，這樣 IK Rule、attachment、hitbox 的對應都不用
額外處理。

```text
root / pelvis / spine_0 / spine_1 / spine_2 / neck / head

clavicle_L / arm_upper_L / arm_lower_L / hand_L
clavicle_R / arm_upper_R / arm_lower_R / hand_R

leg_upper_L / leg_lower_L / ankle_L / ball_L
leg_upper_R / leg_lower_R / ankle_R / ball_R

finger_thumb_0_L .. finger_thumb_2_L
finger_index_0_L .. finger_index_2_L
finger_middle_0_L .. finger_middle_2_L
finger_ring_0_L  .. finger_ring_2_L
finger_pinky_0_L .. finger_pinky_2_L
（右手相同，後綴 _R）
```

如果命名無法對齊，請給一份 `SkeletonMappingProfile`（資源副檔名 `.skmap`），
範例：

```yaml
ProfileId: my-rig
Entries:
  - SourceBone: mixamorig:LeftHand
    TargetBone: hand_L
  - SourceBone: mixamorig:LeftForeArm
    TargetBone: arm_lower_L
  - SourceBone: mixamorig:LeftArm
    TargetBone: arm_upper_L
  - SourceBone: mixamorig:LeftShoulder
    TargetBone: clavicle_L
```

詳見 [`Code/VRLogic/SkeletonMappingProfile.cs`](../Code/VRLogic/SkeletonMappingProfile.cs)
與 [`Code/Player/Services/DefaultRigRebinder.cs`](../Code/Player/Services/DefaultRigRebinder.cs)。

## 4. 動畫需求

s&box 官方 ModelDoc 文件指出：

> Unless your model is meant to be fully static, it should have at least one
> animation sequence. Just a bindPose should be enough: a simple "AnimBindPose"
> node will be enough. Otherwise, for optimization purposes, some things may
> invisibly break (morph targets won't work, IK data will mysteriously go
> missing, etc.)

所以**FBX 至少要有一個動畫 take**，最簡單就是 1 frame 的 T-pose / A-pose。
如果靠 Citizen base model 接管動畫，這條仍要做（避免 IK 資料消失）。

實機進階建議：

- 若模型也要支援第三人稱跑跳，請補 walk / run / jump / idle，遵循
  Citizen animgraph 的參數慣例（`b_grounded`、`move_x/y/z`、`move_bob` 等）。
- 不需要附手部動畫；VR 端會用 `IControllerInput.GetFingerCurl` /
  `GetFingerSplay` 或 OpenXR 骨骼資料驅動。

## 5. animgraph

VR 控制系統 **依賴官方 IK Rule**：手腳兩端都走 `SkinnedModelRenderer.SetIk`。

最省事路徑：在你的 `vmdl` 設定 `Base Model = models/citizen_human/citizen_human_male.vmdl`，
就會自動繼承官方 animgraph 的所有 IK Rule。專案內 `Assets/VRCitizen.vmdl`
就是這個範例：

```kv3
rootNode = {
    _class = "RootNode"
    base_model_name = "models/citizen_human/citizen_human_male.vmdl"
}
```

如果一定要自製 animgraph，那必須包含這些 `CFloat3AnimParameter` /
`CQuaternionAnimParameter` / `CBoolAnimParameter`，名稱完全比對：

| 參數 | 用途 |
| --- | --- |
| `ik.hand_left.position` / `.rotation` / `.enabled` | 左手 IK target |
| `ik.hand_right.position` / `.rotation` / `.enabled` | 右手 IK target |
| `ik.foot_left.position` / `.rotation` / `.enabled` | 左腳 IK target |
| `ik.foot_right.position` / `.rotation` / `.enabled` | 右腳 IK target |

> 程式 `Target.SetIk("hand_left", t)` 內部會把 transform 拆成
> `ik.hand_left.position` / `.rotation` 並把 `.enabled` 設為 `true`，
> `ClearIk("hand_left")` 把 `.enabled` 設為 `false`。

加上對應的 IK Rule（CCD 或 Two-Bone IK 都可），chain 設定：

| Key | Chain |
| --- | --- |
| `hand_left` | `clavicle_L → arm_upper_L → arm_lower_L → hand_L` |
| `hand_right` | `clavicle_R → arm_upper_R → arm_lower_R → hand_R` |
| `foot_left` | `pelvis → leg_upper_L → leg_lower_L → ankle_L` |
| `foot_right` | `pelvis → leg_upper_R → leg_lower_R → ankle_R` |

## 6. ModelDoc Attachments

VR 抓取與 holster 機制都用 attachment。請在 ModelDoc 加入：

| Attachment 名稱 | 位置建議 | 用途 |
| --- | --- | --- |
| `weapon_hold` | 右手手腕（持槍時的虛擬點） | [`AttachmentFirstGrabPoseResolver`](../Code/Player/Services/AttachmentFirstGrabPoseResolver.cs) 在抓取武器時對齊握持姿勢；常數見 [`VrInteractionConstants.cs`](../Code/VRLogic/VrInteractionConstants.cs) |
| `back_rifle` | 上背中央 | 步槍掛回背後 |
| `hip_holster_l` | 左腰 | 副武器 / 手槍掛回 |
| `hip_holster_r` | 右腰 | 同上 |
| `chest_pistol`（可選） | 胸前 | 額外 holster |
| `back_pack`（可選） | 背包點 | 物品掛載 |

身體的 `SkinnedModelRenderer` 必須開 `CreateAttachments = true`，否則
`GetAttachmentObject(...)` / `GetAttachment(...)` 會回 null，
[`VRHolsterSlot`](../Code/Player/Services/VRHolsterSlot.cs) 跟
[`AttachmentFirstGrabPoseResolver`](../Code/Player/Services/AttachmentFirstGrabPoseResolver.cs)
都接不到掛點。

## 7. Hitbox 與傷害倍率

`BulletProjectile` 會讀 hitbox 上的 tag 解析傷害倍率（任何能被
`float.TryParse` 解析的 tag 都會被當倍率使用）：

| Hitbox tag 例 | 意義 |
| --- | --- |
| `head` + `2.0` | 爆頭 2 倍傷害 |
| `body` + `1.0` | 軀幹基礎傷害 |
| `arm` + `0.7` | 手臂 0.7 倍傷害 |
| `leg` + `0.6` | 腿 0.6 倍傷害 |

對應實作：[`Code/Weapons/Base/BulletProjectile.cs`](../Code/Weapons/Base/BulletProjectile.cs)：

```csharp
foreach( string s in tags )
{
    if ( float.TryParse( s, out damageMult ) ) break;
}
```

如果模型代表的角色要能被打中，根 GameObject 還要掛
[`HealthComponent`](../Code/HealthComponent.cs)。

## 8. 手部設定

| 模式 | 模型需要 |
| --- | --- |
| **手指彎曲（curl）** | 至少有 5 根手指、每根 2-3 節 |
| **完整骨骼手追（OpenXR / SteamVR）** | 完整手骨；在 prefab 把 [`VRAnimationHelper.VRHand.UseSkeletalJoints`](../Code/Player/VRAnimationHelper.cs) 勾起來、`JointBones` 字典逐一指到 `Sandbox.VR.VRHandJoint` |
| **官方手 Sandbox.VR.VRHand** | 不需要自模型有手，由 [`OfficialHandToggle`](../Code/Player/Services/OfficialHandToggle.cs) 在「沒拿東西」時切換顯示 |

## 9. 重量分類 tag（讓 grip / 物理手感與物品聯動）

物品根上加下列其一（可省，會 fallback 到 `Rigidbody.Mass`）：

| Tag | 用途 |
| --- | --- |
| `vr_weight_light` | 輕物，grip 跟得緊 |
| `vr_weight_medium` | 中量，預設 |
| `vr_weight_heavy` | 重物，`UsePhysicalHand` 開啟時會明顯把手往下拉 |

實作見 [`MassBasedWeightProfileProvider`](../Code/Player/Services/MassBasedWeightProfileProvider.cs)。

## 10. 交付前檢查清單

- [ ] FBX 為 binary 編碼。
- [ ] T-pose 或 A-pose，朝向正確，比例接近真人。
- [ ] 至少一個動畫 take（不可全靜態）。
- [ ] 骨骼命名與 Citizen 一致，或附 `.skmap`。
- [ ] `SkinnedModelRenderer.CreateAttachments = true`。
- [ ] ModelDoc 內已加 `weapon_hold`、`back_rifle`、`hip_holster_l`、
      `hip_holster_r`。
- [ ] 主要 hitbox（head / body / arm / leg）有 tag，並含傷害倍率 float。
- [ ] 若自製 animgraph，已包含 `ik.hand_left/right` 與 `ik.foot_left/right`
      的 6 個 sub-parameters；若用 base model，已指定為
      `models/citizen_human/citizen_human_male.vmdl`。
- [ ] 材質 / 貼圖獨立打包，無遺漏。
- [ ] 必要的話，提供 `Base Pose` 截圖讓接收端對齊。

## 11. 相關檔案索引

| 檔案 | 用途 |
| --- | --- |
| [`docs/VR_OFFICIAL_IK_MIGRATION.md`](VR_OFFICIAL_IK_MIGRATION.md) | EasyIK → 官方 IK 遷移細節 |
| [`docs/VR_OFFICIAL_API_INTEGRATION.md`](VR_OFFICIAL_API_INTEGRATION.md) | 2026-05-08 起的官方 VR API 整合（input / hand tracking / holster） |
| [`docs/VR_DI_ARCHITECTURE.md`](VR_DI_ARCHITECTURE.md) | DI 架構總覽 |
| [`Code/VRLogic/OfficialArmIkRouter.cs`](../Code/VRLogic/OfficialArmIkRouter.cs) | 官方手臂 IK 純路由邏輯 |
| [`Code/Player/Services/AttachmentFirstGrabPoseResolver.cs`](../Code/Player/Services/AttachmentFirstGrabPoseResolver.cs) | 透過 `weapon_hold` 對齊抓握姿勢 |
| [`Code/Player/Services/VRHolsterSlot.cs`](../Code/Player/Services/VRHolsterSlot.cs) | 用 attachment 點掛載物品 |
