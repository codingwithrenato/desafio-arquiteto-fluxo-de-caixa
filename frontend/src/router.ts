import { createRouter, createWebHistory } from "vue-router";
import ConsoleView from "./views/ConsoleView.vue";
import ExtratoView from "./views/ExtratoView.vue";

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: "/", name: "console", component: ConsoleView },
    { path: "/extrato", name: "extrato", component: ExtratoView },
  ],
});
