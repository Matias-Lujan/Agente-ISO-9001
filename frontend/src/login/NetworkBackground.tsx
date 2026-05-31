// ============================================================================
//  Fondo animado de red de nodos conectados.
//  Canvas con requestAnimationFrame — reusable: recibe config para adaptar
//  colores, densidad y velocidad. Usa ResizeObserver para precision por
//  contenedor (no window resize).
// ============================================================================
import React from 'react';
import { useEffect, useRef, type CSSProperties } from 'react';

export interface NetworkConfig {
  nodeCount?: number;
  connectionRadius?: number;
  nodeSpeed?: number;
  nodeRadius?: number;
  nodeColor?: string;
  lineColor?: string;
}

const DEFAULT_CONFIG: Required<NetworkConfig> = {
  nodeCount:        55,
  connectionRadius: 130,
  nodeSpeed:        0.3,
  nodeRadius:       1.8,
  nodeColor:        'rgba(107, 79, 212, 0.55)',
  lineColor:        'rgba(107, 79, 212, 0.18)',
};

interface Node {
  x: number;
  y: number;
  vx: number;
  vy: number;
}

interface Props {
  config?: NetworkConfig;
  className?: string;
  style?: CSSProperties;
}

export default function NetworkBackground({ config = {}, className = '', style = {} }: Props) {
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const animRef   = useRef<number | null>(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const cfg: Required<NetworkConfig> = { ...DEFAULT_CONFIG, ...config };
    let nodes: Node[] = [];
    let width  = 0;
    let height = 0;

    const resize = () => {
      width  = canvas.width  = canvas.offsetWidth;
      height = canvas.height = canvas.offsetHeight;
    };
    resize();

    const ro = new ResizeObserver(resize);
    ro.observe(canvas);

    nodes = Array.from({ length: cfg.nodeCount }, () => ({
      x:  Math.random() * width,
      y:  Math.random() * height,
      vx: (Math.random() - 0.5) * cfg.nodeSpeed,
      vy: (Math.random() - 0.5) * cfg.nodeSpeed,
    }));

    const draw = () => {
      ctx.clearRect(0, 0, width, height);

      nodes.forEach((n) => {
        n.x += n.vx;
        n.y += n.vy;
        if (n.x < 0 || n.x > width)  n.vx *= -1;
        if (n.y < 0 || n.y > height) n.vy *= -1;
      });

      // Conexiones
      for (let i = 0; i < nodes.length; i++) {
        for (let j = i + 1; j < nodes.length; j++) {
          const dx   = nodes[i].x - nodes[j].x;
          const dy   = nodes[i].y - nodes[j].y;
          const dist = Math.sqrt(dx * dx + dy * dy);
          if (dist < cfg.connectionRadius) {
            const alpha = (1 - dist / cfg.connectionRadius) * 0.18;
            ctx.beginPath();
            ctx.strokeStyle = cfg.lineColor.replace(/[\d.]+\)$/, `${alpha})`);
            ctx.lineWidth   = 0.8;
            ctx.moveTo(nodes[i].x, nodes[i].y);
            ctx.lineTo(nodes[j].x, nodes[j].y);
            ctx.stroke();
          }
        }
      }

      // Nodos
      nodes.forEach((n) => {
        ctx.beginPath();
        ctx.arc(n.x, n.y, cfg.nodeRadius, 0, Math.PI * 2);
        ctx.fillStyle = cfg.nodeColor;
        ctx.fill();
      });

      animRef.current = requestAnimationFrame(draw);
    };

    draw();

    return () => {
      if (animRef.current !== null) cancelAnimationFrame(animRef.current);
      ro.disconnect();
    };
  }, [config]);

  return (
    <canvas
      ref={canvasRef}
      className={className}
      style={{
        position: 'absolute',
        inset:    0,
        width:    '100%',
        height:   '100%',
        display:  'block',
        ...style,
      }}
      aria-hidden="true"
    />
  );
}