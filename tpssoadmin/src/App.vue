<template>
  <router-view />
</template>
<style>
body {
  margin: 0;
  padding: 0;
  background-color: #f5f7fa;
  overflow-x: hidden;
}
input:-webkit-autofill,
input:-webkit-autofill:hover,
input:-webkit-autofill:focus,
input:-webkit-autofill:active {
  -webkit-box-shadow: 0 0 0 30px white inset !important; /* 白色背景，可换成你需要的颜色 */
  box-shadow: 0 0 0 30px white inset !important;
  -webkit-text-fill-color: #333 !important; /* 文字颜色也强制 */
}
</style>