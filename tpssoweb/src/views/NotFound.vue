<template>
  <div class="not-found">
    <div class="content">
      <h1>404</h1>
      <p>抱歉，您访问的页面不存在</p>
      <el-button type="primary" @click="$router.push('/')">返回首页</el-button>
    </div>
  </div>
</template>

<script setup lang="ts">
</script>

<style scoped>
.not-found {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #f0f5ff 0%, #e8f0fe 50%, #f5f0ff 100%);
}

.content {
  text-align: center;
}

h1 {
  font-size: 120px;
  font-weight: 700;
  color: #409eff;
  margin: 0;
  line-height: 1;
}

p {
  font-size: 18px;
  color: #666;
  margin: 16px 0 32px;
}
</style>
