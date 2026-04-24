# com.spacetime.gpuskin.sample

GPU 蒙皮动画 Demo 工程，演示将骨骼/顶点动画烘焙到贴图并在 Shader 中采样，配合 GPU Instancing 实现大批量角色低 Draw Call 渲染。

## 子模块

| 包 | 路径 | 说明 | 文档 |
|---|---|---|---|
| `com.spacetime.gpuskin` | `Packages/com.spacetime.gpuskin` | GPU 蒙皮核心包（骨骼/顶点两种方案） | [README](https://github.com/xieliujian/com.spacetime.gpuskin/blob/main/README.md) |
| `com.spacetime.core` | `Packages/com.spacetime.core` | 基础工具库 | — |

## 方案说明

- **GPUSkinBone**：骨骼矩阵烘焙到 `_BoneAnimTex`，顶点着色器按帧索引采样蒙皮
- **GPUSkinVertex**：顶点位移烘焙到 `_VertexAnimTex`，直接偏移顶点位置
- 两种方案均支持 GPU Instancing，通过全局帧索引 `g_GpuSkinFrameIndex` 统一驱动动画帧
