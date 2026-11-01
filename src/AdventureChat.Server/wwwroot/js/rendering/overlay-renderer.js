export class OverlayRenderer {
    drawDebugOverlay(ctx, room, characters) {
        ctx.save();
        // A. Outer Boundary
        const outer = room.geometry?.outerBoundary;
        if (outer && outer.length >= 3 && outer[0]) {
            ctx.fillStyle = 'rgba(72, 187, 120, 0.18)';
            ctx.strokeStyle = 'rgba(72, 187, 120, 0.85)';
            ctx.lineWidth = 1.5;
            ctx.beginPath();
            ctx.moveTo(outer[0].x, outer[0].y);
            for (let i = 1; i < outer.length; i++) {
                const pt = outer[i];
                if (pt) {
                    ctx.lineTo(pt.x, pt.y);
                }
            }
            ctx.closePath();
            ctx.fill();
            ctx.stroke();
        }
        // B. Obstacles
        if (room.geometry?.obstacles) {
            for (const obstacle of room.geometry.obstacles) {
                if (!obstacle || obstacle.length < 3 || !obstacle[0])
                    continue;
                ctx.fillStyle = 'rgba(245, 101, 101, 0.35)';
                ctx.strokeStyle = 'rgba(245, 101, 101, 0.9)';
                ctx.lineWidth = 1.5;
                ctx.beginPath();
                ctx.moveTo(obstacle[0].x, obstacle[0].y);
                for (let i = 1; i < obstacle.length; i++) {
                    const pt = obstacle[i];
                    if (pt) {
                        ctx.lineTo(pt.x, pt.y);
                    }
                }
                ctx.closePath();
                ctx.fill();
                ctx.stroke();
            }
        }
        // C. Active Movement Paths for all moving characters
        for (const character of characters) {
            if (character.target && character.state === 'walk') {
                ctx.strokeStyle =
                    character.id.startsWith('local') || character.id === characters[0]?.id
                        ? 'rgba(236, 201, 75, 0.85)'
                        : 'rgba(99, 179, 237, 0.85)';
                ctx.lineWidth = 1.5;
                ctx.setLineDash([4, 4]);
                ctx.beginPath();
                ctx.moveTo(character.position.x, character.position.y);
                ctx.lineTo(character.target.x, character.target.y);
                for (const wp of character.waypoints) {
                    ctx.lineTo(wp.x, wp.y);
                }
                ctx.stroke();
                ctx.setLineDash([]);
            }
            // Footpoint Indicator
            ctx.strokeStyle = '#ecc94b';
            ctx.lineWidth = 1;
            const fx = Math.round(character.position.x);
            const fy = Math.round(character.position.y);
            ctx.beginPath();
            ctx.moveTo(fx - 4, fy);
            ctx.lineTo(fx + 4, fy);
            ctx.moveTo(fx, fy - 4);
            ctx.lineTo(fx, fy + 4);
            ctx.stroke();
        }
        // D. Exits and Door Hitboxes
        if (room.exits) {
            for (const exit of room.exits) {
                ctx.strokeStyle = 'rgba(236, 201, 75, 0.9)';
                ctx.fillStyle = 'rgba(236, 201, 75, 0.2)';
                ctx.lineWidth = 1.5;
                if (exit.bounds) {
                    ctx.fillRect(exit.bounds.x, exit.bounds.y, exit.bounds.width, exit.bounds.height);
                    ctx.strokeRect(exit.bounds.x, exit.bounds.y, exit.bounds.width, exit.bounds.height);
                }
                else {
                    ctx.beginPath();
                    ctx.arc(exit.position.x, exit.position.y, exit.radius || 30, 0, Math.PI * 2);
                    ctx.fill();
                    ctx.stroke();
                }
                // Spawn / Target Point
                ctx.fillStyle = '#ecc94b';
                ctx.beginPath();
                ctx.arc(exit.position.x, exit.position.y, 4, 0, Math.PI * 2);
                ctx.fill();
                // Label
                ctx.font = '10px sans-serif';
                ctx.fillStyle = '#ffffff';
                ctx.textAlign = 'center';
                const labelText = `Door: ${exit.label || exit.targetRoomId}`;
                const labelY = exit.bounds ? exit.bounds.y - 4 : exit.position.y - (exit.radius || 30) - 4;
                ctx.fillText(labelText, exit.bounds ? exit.bounds.x + exit.bounds.width / 2 : exit.position.x, labelY);
            }
        }
        ctx.restore();
    }
    drawExits(ctx, room) {
        if (!room.exits || room.exits.length === 0)
            return;
        ctx.save();
        for (const exit of room.exits) {
            if (exit.bounds) {
                // Draw subtle illuminated floor/door frame glow
                const glowGrad = ctx.createRadialGradient(exit.position.x, exit.position.y, 5, exit.position.x, exit.position.y, 35);
                glowGrad.addColorStop(0, 'rgba(255, 220, 100, 0.12)');
                glowGrad.addColorStop(1, 'rgba(255, 220, 100, 0)');
                ctx.fillStyle = glowGrad;
                ctx.beginPath();
                ctx.ellipse(exit.position.x, exit.position.y, 30, 12, 0, 0, Math.PI * 2);
                ctx.fill();
            }
        }
        ctx.restore();
    }
    drawHoveredExitAction(ctx, exit) {
        ctx.save();
        // Pulse highlight around the doorway
        const pulse = 0.5 + Math.sin(performance.now() * 0.008) * 0.5;
        if (exit.bounds) {
            ctx.strokeStyle = `rgba(255, 224, 102, ${0.4 + pulse * 0.4})`;
            ctx.lineWidth = 2;
            ctx.setLineDash([6, 4]);
            ctx.strokeRect(exit.bounds.x, exit.bounds.y, exit.bounds.width, exit.bounds.height);
            ctx.setLineDash([]);
            ctx.fillStyle = `rgba(255, 224, 102, ${0.05 + pulse * 0.08})`;
            ctx.fillRect(exit.bounds.x, exit.bounds.y, exit.bounds.width, exit.bounds.height);
        }
        else {
            ctx.strokeStyle = `rgba(255, 224, 102, ${0.4 + pulse * 0.4})`;
            ctx.lineWidth = 2;
            ctx.setLineDash([6, 4]);
            ctx.beginPath();
            ctx.arc(exit.position.x, exit.position.y, exit.radius || 35, 0, Math.PI * 2);
            ctx.stroke();
            ctx.setLineDash([]);
        }
        ctx.restore();
    }
    drawConnectionStatus(ctx, status) {
        ctx.save();
        ctx.font = '11px sans-serif';
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        const text = status === 'connecting'
            ? 'Verbindung wird hergestellt...'
            : 'Verbindung getrennt. Wiederverbindung...';
        const textWidth = ctx.measureText(text).width;
        const badgeWidth = textWidth + 24;
        const badgeHeight = 22;
        const x = 320;
        const y = 20;
        ctx.fillStyle = 'rgba(0, 0, 0, 0.85)';
        ctx.fillRect(x - badgeWidth / 2, y - badgeHeight / 2, badgeWidth, badgeHeight);
        ctx.fillStyle = status === 'connecting' ? '#ecc94b' : '#fc8181';
        ctx.fillText(text, x, y);
        ctx.restore();
    }
    drawClickMarkers(ctx, clickMarkers, now) {
        const activeMarkers = clickMarkers.filter((m) => now - m.startTime < m.duration);
        ctx.save();
        for (const marker of activeMarkers) {
            const progress = (now - marker.startTime) / marker.duration;
            const alpha = Math.max(0, 1 - progress);
            if (marker.type === 'valid') {
                const radius = 3 + progress * 6;
                ctx.strokeStyle = `rgba(72, 187, 120, ${alpha * 0.9})`;
                ctx.lineWidth = 1.5;
                ctx.beginPath();
                ctx.arc(marker.x, marker.y, radius, 0, Math.PI * 2);
                ctx.stroke();
                ctx.fillStyle = `rgba(72, 187, 120, ${alpha})`;
                ctx.beginPath();
                ctx.arc(marker.x, marker.y, 2, 0, Math.PI * 2);
                ctx.fill();
            }
            else {
                const size = 5 + progress * 2;
                ctx.strokeStyle = `rgba(245, 101, 101, ${alpha})`;
                ctx.lineWidth = 2;
                ctx.beginPath();
                ctx.moveTo(marker.x - size, marker.y - size);
                ctx.lineTo(marker.x + size, marker.y + size);
                ctx.moveTo(marker.x + size, marker.y - size);
                ctx.lineTo(marker.x - size, marker.y + size);
                ctx.stroke();
                const ringRadius = 4 + progress * 8;
                ctx.strokeStyle = `rgba(245, 101, 101, ${alpha * 0.6})`;
                ctx.lineWidth = 1;
                ctx.beginPath();
                ctx.arc(marker.x, marker.y, ringRadius, 0, Math.PI * 2);
                ctx.stroke();
            }
        }
        ctx.restore();
        return activeMarkers;
    }
    drawTargetMarker(ctx, x, y) {
        ctx.save();
        ctx.strokeStyle = 'rgba(255, 230, 100, 0.7)';
        ctx.lineWidth = 1.5;
        ctx.beginPath();
        ctx.arc(x, y, 5, 0, Math.PI * 2);
        ctx.stroke();
        ctx.fillStyle = 'rgba(255, 230, 100, 0.9)';
        ctx.beginPath();
        ctx.arc(x, y, 2, 0, Math.PI * 2);
        ctx.fill();
        ctx.restore();
    }
}
//# sourceMappingURL=overlay-renderer.js.map