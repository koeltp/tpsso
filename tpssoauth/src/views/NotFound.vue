<template>
  <div class="not-found">
    <h1>404</h1>
    <p>页面未找到</p>
    <el-button type="primary" @click="$router.push('/login')">返回登录</el-button>
  </div>
</template>

<style scoped>
.not-found {
  text-align: center;
  padding: 40px 0;
}

.not-found h1 {
  font-size: 72px;
  color: #c0c4cc;
  margin: 0 0 10px;
}

.not-found p {
  font-size: 16px;
  color: #999;
  margin-bottom: 24px;
}
</style>
