<template>
  <div class="guest-container">
    <div class="bubble bubble-1"></div>
    <div class="bubble bubble-2"></div>
    <div class="bubble bubble-3"></div>
    <div class="bubble bubble-4"></div>
    <div class="bubble bubble-5"></div>
    <div class="guest-box">
      <router-view />
    </div>
  </div>
</template>

<style scoped>
.guest-container {
  width: 100%;
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #f0f5ff 0%, #e8f0fe 50%, #f5f0ff 100%);
  position: relative;
  overflow: hidden;
  box-sizing: border-box;
  padding: 40px 20px;
}

.guest-container::before {
  content: '';
  position: absolute;
  top: -250px;
  right: -200px;
  width: 600px;
  height: 600px;
  background: radial-gradient(circle, rgba(124, 58, 237, 0.1) 0%, transparent 70%);
  border-radius: 50%;
}

.guest-container::after {
  content: '';
  position: absolute;
  bottom: -180px;
  left: -150px;
  width: 500px;
  height: 500px;
  background: radial-gradient(circle, rgba(103, 194, 58, 0.07) 0%, transparent 70%);
  border-radius: 50%;
}

.bubble {
  position: absolute;
  border-radius: 50%;
  z-index: 0;
  animation: float 6s ease-in-out infinite;
}

.bubble-1 {
  width: 80px;
  height: 80px;
  top: 15%;
  left: 8%;
  background: radial-gradient(circle at 30% 30%, rgba(124, 58, 237, 0.12), rgba(124, 58, 237, 0.04));
  animation-delay: 0s;
}

.bubble-2 {
  width: 120px;
  height: 120px;
  top: 60%;
  left: 5%;
  background: radial-gradient(circle at 30% 30%, rgba(124, 58, 237, 0.1), rgba(124, 58, 237, 0.03));
  animation-delay: 1s;
  animation-duration: 8s;
}

.bubble-3 {
  width: 60px;
  height: 60px;
  top: 25%;
  right: 12%;
  background: radial-gradient(circle at 30% 30%, rgba(103, 194, 58, 0.1), rgba(103, 194, 58, 0.03));
  animation-delay: 2s;
  animation-duration: 7s;
}

.bubble-4 {
  width: 100px;
  height: 100px;
  top: 70%;
  right: 8%;
  background: radial-gradient(circle at 30% 30%, rgba(124, 58, 237, 0.08), rgba(124, 58, 237, 0.02));
  animation-delay: 0.5s;
  animation-duration: 9s;
}

.bubble-5 {
  width: 40px;
  height: 40px;
  top: 45%;
  left: 50%;
  background: radial-gradient(circle at 30% 30%, rgba(124, 58, 237, 0.12), rgba(124, 58, 237, 0.04));
  animation-delay: 3s;
  animation-duration: 6s;
}

@keyframes float {
  0%, 100% { transform: translateY(0) scale(1); }
  50% { transform: translateY(-20px) scale(1.05); }
}

.guest-box {
  position: relative;
  width: 420px;
  margin: 0 auto;
  padding: 40px;
  background: white;
  border-radius: 16px;
  box-shadow: 0 8px 40px rgba(0, 0, 0, 0.08);
  border: 1px solid #f0f2f5;
  z-index: 1;
}
</style>
